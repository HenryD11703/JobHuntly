using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace JobHuntly.Tests.Integration;

[Collection(ApiCollection.Name)]
public class AuthTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [DatabaseFact] // AC-9.2
    public async Task Register_sets_an_httponly_cookie_and_never_returns_the_token()
    {
        var client = _factory.CreateBrowserClient();

        var response = await ApiFactory.RegisterAsync(client, ApiFactory.NewEmail());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var setCookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("jh_token=", setCookie);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=lax", setCookie, StringComparison.OrdinalIgnoreCase);

        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(body.RootElement.TryGetProperty("token", out _), "The token must not be in the response body.");
    }

    [DatabaseFact] // AC-9.2
    public async Task Cookie_is_marked_secure_outside_development()
    {
        using var production = new ApiFactory("Production");
        var client = production.CreateBrowserClient(https: true);

        var response = await ApiFactory.RegisterAsync(client, ApiFactory.NewEmail());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var setCookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [DatabaseFact]
    public async Task Me_requires_the_cookie_and_works_with_it()
    {
        var anonymous = _factory.CreateBrowserClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync("/api/auth/me")).StatusCode);

        var signedIn = await _factory.CreateSignedInClientAsync();
        Assert.Equal(HttpStatusCode.OK, (await signedIn.GetAsync("/api/auth/me")).StatusCode);
    }

    [DatabaseFact]
    public async Task Logout_clears_the_session()
    {
        var client = await _factory.CreateSignedInClientAsync();

        var logout = await client.PostAsync("/api/auth/logout", content: null);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);
    }

    [DatabaseFact] // AC-9.5
    public async Task Duplicate_email_returns_409_even_with_different_casing()
    {
        var client = _factory.CreateBrowserClient();
        var email = ApiFactory.NewEmail();
        Assert.Equal(HttpStatusCode.OK, (await ApiFactory.RegisterAsync(client, email)).StatusCode);

        var duplicate = await ApiFactory.RegisterAsync(client, email.ToUpperInvariant());

        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);
    }

    [DatabaseFact] // AC-9.5
    public async Task Short_password_and_bad_email_return_422()
    {
        var client = _factory.CreateBrowserClient();

        var shortPassword = await ApiFactory.RegisterAsync(client, ApiFactory.NewEmail(), password: "short");
        var badEmail = await ApiFactory.RegisterAsync(client, "not-an-email");

        Assert.Equal(HttpStatusCode.UnprocessableEntity, shortPassword.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, badEmail.StatusCode);
    }

    [DatabaseFact]
    public async Task Login_works_case_insensitively_and_rejects_a_wrong_password_with_401()
    {
        var email = ApiFactory.NewEmail();
        var client = _factory.CreateBrowserClient();
        await ApiFactory.RegisterAsync(client, email);

        var good = await _factory.CreateBrowserClient().PostAsJsonAsync("/api/auth/login",
            new { email = email.ToUpperInvariant(), password = ApiFactory.ValidPassword });
        var bad = await _factory.CreateBrowserClient().PostAsJsonAsync("/api/auth/login",
            new { email, password = "wrong-password-123" });

        Assert.Equal(HttpStatusCode.OK, good.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, bad.StatusCode);
    }

    [DatabaseFact]
    public async Task State_changing_requests_without_the_csrf_header_are_rejected()
    {
        var client = _factory.CreateClient(); // no X-Requested-With header

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "x@example.test", password = "whatever-123" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [DatabaseFact] // AC-9.6
    public async Task Errors_are_problem_details_without_internals()
    {
        var client = _factory.CreateBrowserClient();

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new { email = "nobody@example.test", password = "whatever-123" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(401, body.RootElement.GetProperty("status").GetInt32());
        Assert.DoesNotContain("Exception", body.RootElement.ToString());
    }

    [DatabaseFact]
    public async Task Health_endpoint_is_public()
    {
        var response = await _factory.CreateBrowserClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
