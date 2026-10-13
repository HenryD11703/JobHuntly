using System.Net;
using System.Net.Http.Json;

namespace JobHuntly.Tests.Integration;

/// <summary>AC-9.1: user B can never read or change user A's data. 404, not 403, so existence does not leak.</summary>
[Collection(ApiCollection.Name)]
public class IsolationTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private sealed record Created(Guid Id);

    private static async Task<Guid> CreateApplicationAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/jobapplications",
            new { companyName = "Acme", positionTitle = "Engineer" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<Created>())!.Id;
    }

    [DatabaseFact] // AC-1.1 (API half)
    public async Task Creating_an_application_needs_only_company_and_position()
    {
        var client = await _factory.CreateSignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/jobapplications",
            new { companyName = "Acme", positionTitle = "Engineer" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var list = await client.GetFromJsonAsync<List<System.Text.Json.JsonElement>>("/api/jobapplications");
        Assert.Single(list!);
        Assert.Equal(0, list![0].GetProperty("status").GetInt32()); // Backlog
    }

    [DatabaseFact]
    public async Task Anonymous_requests_to_data_endpoints_return_401()
    {
        var client = _factory.CreateBrowserClient();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/jobapplications")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/analytics/summary")).StatusCode);
    }

    [DatabaseFact]
    public async Task User_B_does_not_see_or_change_user_A_applications()
    {
        var userA = await _factory.CreateSignedInClientAsync();
        var userB = await _factory.CreateSignedInClientAsync();
        var id = await CreateApplicationAsync(userA);

        var listB = await userB.GetFromJsonAsync<List<System.Text.Json.JsonElement>>("/api/jobapplications");
        Assert.Empty(listB!);

        var status = await userB.PutAsJsonAsync($"/api/jobapplications/{id}/status", new { status = 4 });
        var update = await userB.PutAsJsonAsync($"/api/jobapplications/{id}", new { notes = "hacked" });
        var interview = await userB.PostAsJsonAsync($"/api/jobapplications/{id}/interviews",
            new { interviewDate = DateTime.UtcNow.AddDays(1), roundType = "Technical" });
        var delete = await userB.DeleteAsync($"/api/jobapplications/{id}");

        Assert.Equal(HttpStatusCode.NotFound, status.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, interview.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);

        // And it is intact for its owner.
        var listA = await userA.GetFromJsonAsync<List<System.Text.Json.JsonElement>>("/api/jobapplications");
        Assert.Single(listA!);
        Assert.Equal(0, listA![0].GetProperty("status").GetInt32());
    }

    [DatabaseFact]
    public async Task Invalid_input_returns_422()
    {
        var client = await _factory.CreateSignedInClientAsync();

        var missingCompany = await client.PostAsJsonAsync("/api/jobapplications",
            new { companyName = "", positionTitle = "Engineer" });
        var badStatus = await client.PostAsJsonAsync("/api/jobapplications",
            new { companyName = "Acme", positionTitle = "Engineer", status = 99 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, missingCompany.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, badStatus.StatusCode);
    }

    [DatabaseFact]
    public async Task Unknown_application_returns_404_not_400()
    {
        var client = await _factory.CreateSignedInClientAsync();

        var response = await client.DeleteAsync($"/api/jobapplications/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
