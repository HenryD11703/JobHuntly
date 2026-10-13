using System.Net;
using System.Net.Http.Json;

namespace JobHuntly.Tests.Integration;

/// <summary>AC-9.4. Lives in the shared serial collection because it sets a process-wide variable.</summary>
[Collection(ApiCollection.Name)]
public class RateLimitTests : IDisposable
{
    private const string Variable = "RateLimit__AuthPerMinute";
    private readonly string? _previous = Environment.GetEnvironmentVariable(Variable);

    public RateLimitTests() => Environment.SetEnvironmentVariable(Variable, "3");

    public void Dispose() => Environment.SetEnvironmentVariable(Variable, _previous);

    [DatabaseFact]
    public async Task Login_attempts_beyond_the_limit_return_429()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateBrowserClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 5; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login",
                new { email = "nobody@example.test", password = "whatever-123" });
            statuses.Add(response.StatusCode);
        }

        Assert.Equal(3, statuses.Count(s => s == HttpStatusCode.Unauthorized));
        Assert.Equal(2, statuses.Count(s => s == HttpStatusCode.TooManyRequests));
    }
}
