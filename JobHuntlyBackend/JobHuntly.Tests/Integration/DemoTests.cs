using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JobHuntly.Infrastructure.Data;
using JobHuntly.Infrastructure.Demo;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace JobHuntly.Tests.Integration;

/// <summary>AC-8.1 to AC-8.6: the one-click demo sandbox.</summary>
[Collection(ApiCollection.Name)]
public class DemoTests : IDisposable
{
    private readonly ApiFactory _factory = new();
    private readonly string? _previousDemoLimit = Environment.GetEnvironmentVariable("RateLimit__DemoPerHour");

    public void Dispose()
    {
        _factory.Dispose();
        Environment.SetEnvironmentVariable("RateLimit__DemoPerHour", _previousDemoLimit);
    }

    private static async Task<JsonElement> OkAsync(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    private static async Task<HttpClient> StartDemoAsync(ApiFactory factory)
    {
        var client = factory.CreateBrowserClient();
        var response = await client.PostAsync("/api/auth/demo", content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return client;
    }

    [DatabaseFact] // AC-8.1
    public async Task One_click_gives_a_signed_in_sandbox_full_of_sample_data()
    {
        var client = _factory.CreateBrowserClient();

        var response = await client.PostAsync("/api/auth/demo", content: null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("max-age=86400", cookie, StringComparison.OrdinalIgnoreCase);   // lasts as long as the sandbox
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("token", body, StringComparison.OrdinalIgnoreCase);

        var applications = await OkAsync(await client.GetAsync("/api/jobapplications"));
        Assert.Equal(14, applications.GetArrayLength());
        var interviews = applications.EnumerateArray().SelectMany(a => a.GetProperty("interviews").EnumerateArray()).ToList();
        Assert.True(interviews.Count >= 3);
        Assert.Contains(interviews, i => i.GetProperty("debrief").ValueKind == JsonValueKind.Object);
        Assert.Equal(2, (await OkAsync(await client.GetAsync("/api/stories"))).GetArrayLength());
        Assert.Equal(3, (await OkAsync(await client.GetAsync("/api/offers"))).GetProperty("offers").GetArrayLength());

        // Each screen has real content: Today lists the three nudges from the mockup.
        var today = await OkAsync(await client.GetAsync("/api/today"));
        Assert.Equal(new[] { "Prepare", "WriteDebrief", "FollowUp" }, today.EnumerateArray().Select(n => n.GetProperty("type").GetString()));

        var insights = await OkAsync(await client.GetAsync("/api/analytics/insights"));
        Assert.Equal(12, insights.GetProperty("appliedApplications").GetInt32());

        // The saved AI prep questions come with the seeded interview, no AI call needed.
        var northwind = applications.EnumerateArray().Single(a => a.GetProperty("companyName").GetString() == "Northwind Labs");
        var technical = northwind.GetProperty("interviews").EnumerateArray().Single(i => i.GetProperty("roundType").GetString() == "Technical");
        Assert.Equal(3, technical.GetProperty("prepQuestions").GetProperty("questions").GetArrayLength());
        Assert.Equal(0, _factory.Ai.Calls);
    }

    [DatabaseFact]
    public async Task The_profile_says_it_is_a_demo_and_when_it_expires()
    {
        var client = await StartDemoAsync(_factory);

        var me = await OkAsync(await client.GetAsync("/api/auth/me"));

        Assert.True(me.GetProperty("isDemo").GetBoolean());
        var expires = me.GetProperty("demoExpiresAt").GetDateTime();
        Assert.InRange((expires - DateTime.UtcNow).TotalHours, 23, 25);
    }

    [DatabaseFact] // AC-8.2
    public async Task Two_demo_sessions_never_see_each_others_changes()
    {
        var a = await StartDemoAsync(_factory);
        var b = await StartDemoAsync(_factory);

        await OkAsync(await a.PostAsJsonAsync("/api/jobapplications", new { companyName = "Only in A", positionTitle = "Tester" }));
        await OkAsync(await a.PostAsJsonAsync("/api/stories", new { title = "Mine", situation = "S", task = "T", action = "A", result = "R" }));
        var firstA = (await OkAsync(await a.GetAsync("/api/jobapplications")))[0].GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await a.DeleteAsync($"/api/jobapplications/{firstA}")).StatusCode);

        var listA = await OkAsync(await a.GetAsync("/api/jobapplications"));
        var listB = await OkAsync(await b.GetAsync("/api/jobapplications"));

        Assert.Equal(14, listA.GetArrayLength());   // +1 added, -1 deleted
        Assert.Equal(14, listB.GetArrayLength());   // untouched
        Assert.DoesNotContain(listB.EnumerateArray(), x => x.GetProperty("companyName").GetString() == "Only in A");
        Assert.Equal(2, (await OkAsync(await b.GetAsync("/api/stories"))).GetArrayLength());
        Assert.Equal(HttpStatusCode.NotFound, (await b.GetAsync($"/api/jobapplications/{firstA}")).StatusCode);
        var idsA = listA.EnumerateArray().Select(x => x.GetProperty("id").GetGuid()).ToHashSet();
        Assert.DoesNotContain(listB.EnumerateArray(), x => idsA.Contains(x.GetProperty("id").GetGuid()));
    }

    [DatabaseFact] // AC-8.3
    public async Task Sandboxes_are_deleted_with_all_their_data_after_24_hours_and_real_accounts_are_untouched()
    {
        var real = await _factory.CreateSignedInClientAsync();
        await real.PostAsJsonAsync("/api/jobapplications", new { companyName = "Real", positionTitle = "Keeps its data" });
        var old = await StartDemoAsync(_factory);
        var oldUserId = (await OkAsync(await old.GetAsync("/api/auth/me"))).GetProperty("id").GetGuid();

        _factory.Time.Advance(TimeSpan.FromHours(23));
        var recent = await StartDemoAsync(_factory);
        var recentUserId = (await OkAsync(await recent.GetAsync("/api/auth/me"))).GetProperty("id").GetGuid();

        _factory.Time.Advance(TimeSpan.FromHours(2));   // old one is 25 h old, recent one 2 h
        using var scope = _factory.Services.CreateScope();
        var deleted = await scope.ServiceProvider.GetRequiredService<DemoCleaner>().DeleteExpiredAsync();

        Assert.True(deleted >= 1);
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.False(await db.Users.AnyAsync(u => u.Id == oldUserId));
        Assert.Equal(0, await db.JobApplications.CountAsync(a => a.UserId == oldUserId));   // cascaded
        Assert.Equal(0, await db.Stories.CountAsync(s => s.UserId == oldUserId));
        Assert.True(await db.Users.AnyAsync(u => u.Id == recentUserId));
        Assert.Equal(14, await db.JobApplications.CountAsync(a => a.UserId == recentUserId));
        Assert.Equal(1, (await OkAsync(await real.GetAsync("/api/jobapplications"))).GetArrayLength());
    }

    [DatabaseFact] // AC-8.5
    public async Task Demo_users_get_the_lower_ai_allowance()
    {
        var demo = await StartDemoAsync(_factory);
        var real = await _factory.CreateSignedInClientAsync();

        var demoStatus = await OkAsync(await demo.GetAsync("/api/ai/status"));
        var realStatus = await OkAsync(await real.GetAsync("/api/ai/status"));

        Assert.Equal(8, demoStatus.GetProperty("dailyLimit").GetInt32());
        Assert.Equal(30, realStatus.GetProperty("dailyLimit").GetInt32());
    }

    [DatabaseFact] // AC-8.6
    public async Task Creating_sandboxes_is_rate_limited_per_ip()
    {
        Environment.SetEnvironmentVariable("RateLimit__DemoPerHour", "2");
        using var limited = new ApiFactory();
        var client = limited.CreateBrowserClient();

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 3; i++) statuses.Add((await client.PostAsync("/api/auth/demo", content: null)).StatusCode);

        Assert.Equal(new[] { HttpStatusCode.OK, HttpStatusCode.OK, HttpStatusCode.TooManyRequests }, statuses);
    }

    [DatabaseFact]
    public async Task A_sandbox_cannot_be_entered_through_the_login_form()
    {
        var client = await StartDemoAsync(_factory);
        var email = (await OkAsync(await client.GetAsync("/api/auth/me"))).GetProperty("email").GetString();

        var attempt = await _factory.CreateBrowserClient().PostAsJsonAsync("/api/auth/login",
            new { email, password = "anything-you-try" });

        Assert.Equal(HttpStatusCode.Unauthorized, attempt.StatusCode);
    }

    [DatabaseFact]
    public async Task The_demo_endpoint_needs_the_csrf_header_like_every_other_write()
    {
        var withoutHeader = _factory.CreateClient();

        var response = await withoutHeader.PostAsync("/api/auth/demo", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
