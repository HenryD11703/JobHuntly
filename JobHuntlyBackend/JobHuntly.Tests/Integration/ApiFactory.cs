using System.Net.Http.Json;
using JobHuntly.Api.Infrastructure;
using JobHuntly.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JobHuntly.Tests.Integration;

/// <summary>
/// Integration tests need a real PostgreSQL. They use the same ConnectionStrings__DefaultConnection
/// as the dev server (from `.env` locally, from the CI service container in GitHub Actions),
/// so point it at a DEVELOPMENT database, never production. Tests create users whose email starts
/// with <see cref="TestEmailPrefix"/> and delete them afterwards.
/// </summary>
public static class TestDatabase
{
    public const string TestEmailPrefix = "itest-";

    static TestDatabase()
    {
        DotEnv.Load();
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("JwtSettings__Secret")))
            Environment.SetEnvironmentVariable("JwtSettings__Secret", "test-only-" + Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable("JwtSettings__Issuer", "JobHuntly");
        Environment.SetEnvironmentVariable("JwtSettings__Audience", "JobHuntlyApp");
    }

    public static bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"));
}

/// <summary>Like [Fact], but skipped (not failed) when no database is configured.</summary>
public sealed class DatabaseFactAttribute : FactAttribute
{
    public DatabaseFactAttribute()
    {
        _ = TestDatabase.IsConfigured;
        if (!TestDatabase.IsConfigured)
            Skip = "Set ConnectionStrings__DefaultConnection (see .env.example) to run integration tests.";
    }
}

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private readonly string _environment;

    public ApiFactory(string environment = "Development")
    {
        _ = TestDatabase.IsConfigured;
        _environment = environment;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseEnvironment(_environment);

    /// <summary>A client that behaves like our frontend: cookies on, CSRF header set.</summary>
    public HttpClient CreateBrowserClient(bool https = false)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            HandleCookies = true,
            BaseAddress = new Uri(https ? "https://localhost" : "http://localhost")
        });
        client.DefaultRequestHeaders.Add(CsrfGuardMiddleware.HeaderName, CsrfGuardMiddleware.HeaderValue);
        return client;
    }

    public static string NewEmail() => $"{TestDatabase.TestEmailPrefix}{Guid.NewGuid():N}@example.test";

    public const string ValidPassword = "correct-horse-battery";

    public static async Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email, string password = ValidPassword) =>
        await client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            firstName = "Test",
            lastName = "User"
        });

    /// <summary>Registers a fresh user and returns a client already holding that user's cookie.</summary>
    public async Task<HttpClient> CreateSignedInClientAsync()
    {
        var client = CreateBrowserClient();
        var response = await RegisterAsync(client, NewEmail());
        response.EnsureSuccessStatusCode();
        return client;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && TestDatabase.IsConfigured)
        {
            try
            {
                using var scope = Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Users.Where(u => u.Email.StartsWith(TestDatabase.TestEmailPrefix)).ExecuteDelete();
            }
            catch
            {
                // Cleanup is best effort: if the host never started (e.g. database unreachable),
                // the original test failure is the useful error, not this one.
            }
        }
        base.Dispose(disposing);
    }
}

/// <summary>All integration tests share one collection so they run serially (they set process env vars).</summary>
[CollectionDefinition(Name)]
public sealed class ApiCollection
{
    public const string Name = "Api";
}
