using System.Net;

namespace JobHuntly.Tests.Integration;

/// <summary>AC-9.3: only the configured frontend origins may call the API from a browser.</summary>
[Collection(ApiCollection.Name)]
public class CorsTests : IDisposable
{
    private const string Variable = "Cors__AllowedOrigins";
    private readonly string? _previous = Environment.GetEnvironmentVariable(Variable);

    public CorsTests() => Environment.SetEnvironmentVariable(Variable, "https://app.example.test, http://localhost:3000");

    public void Dispose() => Environment.SetEnvironmentVariable(Variable, _previous);

    private static async Task<HttpResponseMessage> WithOrigin(HttpClient client, string origin)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", origin);
        return await client.SendAsync(request);
    }

    [DatabaseFact]
    public async Task A_configured_origin_is_allowed_with_credentials()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateBrowserClient();

        var response = await WithOrigin(client, "https://app.example.test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("https://app.example.test", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Origin")));
        Assert.Equal("true", Assert.Single(response.Headers.GetValues("Access-Control-Allow-Credentials")));
    }

    [DatabaseFact]
    public async Task Any_other_origin_gets_no_cors_headers()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateBrowserClient();

        var response = await WithOrigin(client, "https://evil.example");

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [DatabaseFact]
    public async Task A_preflight_from_an_unknown_origin_is_not_approved()
    {
        using var factory = new ApiFactory();
        var client = factory.CreateBrowserClient();
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/jobapplications");
        request.Headers.Add("Origin", "https://evil.example");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        var response = await client.SendAsync(request);

        Assert.False(response.Headers.Contains("Access-Control-Allow-Origin"));
    }
}
