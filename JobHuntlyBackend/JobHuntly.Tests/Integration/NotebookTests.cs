using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace JobHuntly.Tests.Integration;

/// <summary>
/// Interview-notebook behaviour end to end against a real PostgreSQL: timeline, interviews,
/// checklist, debrief, Today nudges, insights, offers, exchange rates and stories.
/// "Now" is controlled by <see cref="ApiFactory.Time"/>.
/// </summary>
[Collection(ApiCollection.Name)]
public class NotebookTests : IDisposable
{
    private readonly ApiFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    private DateTime Now => _factory.Time.GetUtcNow().UtcDateTime;

    // ---------- helpers ----------

    private static async Task<JsonElement> ReadAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }

    private static async Task<JsonElement> OkAsync(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return await ReadAsync(response);
    }

    private static async Task<Guid> CreateApplicationAsync(HttpClient client, string company = "Acme", string status = "Backlog", string? source = null)
    {
        var response = await client.PostAsJsonAsync("/api/jobapplications",
            new { companyName = company, positionTitle = "Engineer", status, source });
        return (await OkAsync(response)).GetProperty("id").GetGuid();
    }

    private async Task<Guid> AddInterviewAsync(HttpClient client, Guid applicationId, DateTime date, string round = "Technical")
    {
        var response = await client.PostAsJsonAsync($"/api/jobapplications/{applicationId}/interviews",
            new { interviewDate = date, roundType = round });
        return (await OkAsync(response)).GetProperty("id").GetGuid();
    }

    private static async Task<List<JsonElement>> ListAsync(HttpClient client, string url) =>
        (await OkAsync(await client.GetAsync(url))).EnumerateArray().ToList();

    private static async Task<List<string>> NudgeTypesAsync(HttpClient client) =>
        (await ListAsync(client, "/api/today")).Select(n => n.GetProperty("type").GetString()!).ToList();

    // ---------- applications, source, timeline ----------

    [DatabaseFact] // AC-1.2
    public async Task Source_is_optional_and_invalid_values_return_422()
    {
        var client = await _factory.CreateSignedInClientAsync();

        var withSource = await client.PostAsJsonAsync("/api/jobapplications",
            new { companyName = "Acme", positionTitle = "Engineer", source = "LinkedIn" });
        var without = await client.PostAsJsonAsync("/api/jobapplications",
            new { companyName = "Beta", positionTitle = "Engineer" });
        var invalid = await client.PostAsJsonAsync("/api/jobapplications",
            new { companyName = "Gamma", positionTitle = "Engineer", source = "Fax" });

        Assert.Equal("LinkedIn", (await OkAsync(withSource)).GetProperty("source").GetString());
        Assert.Equal(JsonValueKind.Null, (await OkAsync(without)).GetProperty("source").ValueKind);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalid.StatusCode);
    }

    [DatabaseFact] // AC-4.4 and timeline events
    public async Task Status_changes_are_recorded_in_the_timeline_and_rejection_reason_is_optional()
    {
        var client = await _factory.CreateSignedInClientAsync();
        var id = await CreateApplicationAsync(client);

        await OkAsync(await client.PutAsJsonAsync($"/api/jobapplications/{id}/status", new { status = "Applied" }));
        _factory.Time.Advance(TimeSpan.FromDays(3));
        var rejected = await OkAsync(await client.PutAsJsonAsync($"/api/jobapplications/{id}/status",
            new { status = "Rejected", rejectionReason = "Skills gap" }));
        var noReason = await CreateApplicationAsync(client, "Other", "Applied");
        var rejectedWithout = await OkAsync(await client.PutAsJsonAsync($"/api/jobapplications/{noReason}/status", new { status = "Rejected" }));

        Assert.Equal("Skills gap", rejected.GetProperty("rejectionReason").GetString());
        Assert.Equal(JsonValueKind.Null, rejectedWithout.GetProperty("rejectionReason").ValueKind);

        var timeline = await ListAsync(client, $"/api/jobapplications/{id}/timeline");
        var types = timeline.Select(e => e.GetProperty("type").GetString()).ToList();
        Assert.Equal(new[] { "StatusChanged", "StatusChanged", "Created" }, types); // newest first
        Assert.Equal("Rejected", timeline[0].GetProperty("toStatus").GetString());
        Assert.Equal("Applied", timeline[0].GetProperty("fromStatus").GetString());
    }

    [DatabaseFact]
    public async Task Setting_the_same_status_again_adds_no_event()
    {
        var client = await _factory.CreateSignedInClientAsync();
        var id = await CreateApplicationAsync(client, status: "Applied");

        await OkAsync(await client.PutAsJsonAsync($"/api/jobapplications/{id}/status", new { status = "Applied" }));

        Assert.Single(await ListAsync(client, $"/api/jobapplications/{id}/timeline"));
    }

    [DatabaseFact]
    public async Task Logging_a_follow_up_adds_an_event()
    {
        var client = await _factory.CreateSignedInClientAsync();
        var id = await CreateApplicationAsync(client, status: "Applied");

        var evt = await OkAsync(await client.PostAsJsonAsync($"/api/jobapplications/{id}/follow-ups", new { note = "Emailed the recruiter" }));

        Assert.Equal("FollowedUp", evt.GetProperty("type").GetString());
        Assert.Equal("Emailed the recruiter", evt.GetProperty("note").GetString());
    }

    // ---------- interviews, checklist, debrief ----------

    [DatabaseFact] // AC-3.1, AC-3.2
    public async Task Interviews_have_a_round_a_status_and_a_checklist_that_persists()
    {
        var client = await _factory.CreateSignedInClientAsync();
        var appId = await CreateApplicationAsync(client, status: "Interviewing");
        var interviewId = await AddInterviewAsync(client, appId, Now.AddHours(30));

        var url = $"/api/jobapplications/{appId}/interviews/{interviewId}/checklist";
        var toggled = await OkAsync(await client.PutAsJsonAsync(url, new { itemId = "setup", done = true }));

        Assert.Equal("Technical", toggled.GetProperty("roundType").GetString());
        Assert.Equal("Scheduled", toggled.GetProperty("status").GetString());
        var checklist = toggled.GetProperty("checklist").EnumerateArray().ToList();
        Assert.Equal(5, checklist.Count);
        Assert.Equal(new[] { "setup" }, checklist.Where(c => c.GetProperty("done").GetBoolean()).Select(c => c.GetProperty("id").GetString()));

        // It persists: a fresh read of the application shows the same tick.
        var app = await OkAsync(await client.GetAsync($"/api/jobapplications/{appId}"));
        var stored = app.GetProperty("interviews")[0].GetProperty("checklist").EnumerateArray()
            .Single(c => c.GetProperty("id").GetString() == "setup");
        Assert.True(stored.GetProperty("done").GetBoolean());

        // Untick works too.
        var untick = await OkAsync(await client.PutAsJsonAsync(url, new { itemId = "setup", done = false }));
        Assert.DoesNotContain(untick.GetProperty("checklist").EnumerateArray(), c => c.GetProperty("done").GetBoolean());
    }

    [DatabaseFact]
    public async Task Checklist_rejects_items_that_do_not_belong_to_the_round()
    {
        var client = await _factory.CreateSignedInClientAsync();
        var appId = await CreateApplicationAsync(client, status: "Interviewing");
        var interviewId = await AddInterviewAsync(client, appId, Now.AddHours(30), "Technical");

        var wrong = await client.PutAsJsonAsync($"/api/jobapplications/{appId}/interviews/{interviewId}/checklist",
            new { itemId = "salary-range", done = true }); // an HR-screen item

        Assert.Equal(HttpStatusCode.UnprocessableEntity, wrong.StatusCode);
    }

    [DatabaseFact] // AC-4.1, AC-4.3
    public async Task A_debrief_needs_a_past_interview_marks_it_done_and_is_stored_as_structured_data()
    {
        var client = await _factory.CreateSignedInClientAsync();
        var appId = await CreateApplicationAsync(client, status: "Interviewing");
        var interviewId = await AddInterviewAsync(client, appId, Now.AddDays(2), "HrScreen");
        var url = $"/api/jobapplications/{appId}/interviews/{interviewId}/debrief";
        var payload = new
        {
            rating = 4,
            rawNotes = "Went fine",
            questionsAsked = new[] { "Salary expectations", "  ", "Team size" },
            wentWell = new[] { "Salary accepted" },
            wentPoorly = new[] { "Hesitated on team size" },
            followUps = new[] { "Prepare an answer about team size" }
        };

        var tooEarly = await client.PutAsJsonAsync(url, payload);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tooEarly.StatusCode);

        _factory.Time.Advance(TimeSpan.FromDays(3));
        var saved = await OkAsync(await client.PutAsJsonAsync(url, payload));

        Assert.Equal("Done", saved.GetProperty("status").GetString());
        var debrief = saved.GetProperty("debrief");
        Assert.Equal(4, debrief.GetProperty("rating").GetInt32());
        Assert.Equal(new[] { "Salary expectations", "Team size" },
            debrief.GetProperty("questionsAsked").EnumerateArray().Select(q => q.GetString()));

        // Saving again updates in place and does not add another timeline event.
        await OkAsync(await client.PutAsJsonAsync(url, new { rating = 5, rawNotes = "Edited", questionsAsked = Array.Empty<string>(), wentWell = Array.Empty<string>(), wentPoorly = Array.Empty<string>(), followUps = Array.Empty<string>() }));
        var timeline = await ListAsync(client, $"/api/jobapplications/{appId}/timeline");
        Assert.Single(timeline, e => e.GetProperty("type").GetString() == "DebriefSaved");
        var app = await OkAsync(await client.GetAsync($"/api/jobapplications/{appId}"));
        Assert.Equal(5, app.GetProperty("interviews")[0].GetProperty("debrief").GetProperty("rating").GetInt32());
    }

    [DatabaseFact]
    public async Task Debrief_validation_rejects_bad_ratings_and_cancelled_interviews()
    {
        var client = await _factory.CreateSignedInClientAsync();
        var appId = await CreateApplicationAsync(client, status: "Interviewing");
        var interviewId = await AddInterviewAsync(client, appId, Now.AddDays(-1));
        var url = $"/api/jobapplications/{appId}/interviews/{interviewId}/debrief";

        var badRating = await client.PutAsJsonAsync(url, new { rating = 9, rawNotes = "x" });
        await OkAsync(await client.PutAsJsonAsync($"/api/jobapplications/{appId}/interviews/{interviewId}", new { status = "Cancelled" }));
        var cancelled = await client.PutAsJsonAsync(url, new { rating = 3, rawNotes = "x" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, badRating.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, cancelled.StatusCode);
    }

    [DatabaseFact] // naive dates must not crash PostgreSQL timestamptz
    public async Task Dates_without_an_offset_are_accepted_as_utc()
    {
        var client = await _factory.CreateSignedInClientAsync();
        var appId = await CreateApplicationAsync(client, status: "Interviewing");

        var response = await client.PostAsJsonAsync($"/api/jobapplications/{appId}/interviews",
            new { interviewDate = "2026-10-15T10:00:00", roundType = "Final" });

        var interview = await OkAsync(response);
        Assert.StartsWith("2026-10-15T10:00:00", interview.GetProperty("interviewDate").GetString());
    }

    // ---------- Today ----------

    [DatabaseFact] // AC-5.1, AC-5.2
    public async Task Follow_up_nudge_appears_at_seven_days_and_disappears_after_logging_a_follow_up()
    {
        var client = await _factory.CreateSignedInClientAsync();
        var id = await CreateApplicationAsync(client, status: "Applied");
        Assert.Empty(await NudgeTypesAsync(client));

        _factory.Time.Advance(TimeSpan.FromDays(7).Subtract(TimeSpan.FromMinutes(1)));
        Assert.Empty(await NudgeTypesAsync(client));

        _factory.Time.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(new[] { "FollowUp" }, await NudgeTypesAsync(client));

        await OkAsync(await client.PostAsJsonAsync($"/api/jobapplications/{id}/follow-ups", new { }));
        Assert.Empty(await NudgeTypesAsync(client));
    }

    [DatabaseFact] // AC-5.1, AC-5.2
    public async Task Prepare_then_debrief_nudges_follow_the_interview_lifecycle()
    {
        var client = await _factory.CreateSignedInClientAsync();
        var appId = await CreateApplicationAsync(client, status: "Interviewing");
        var interviewId = await AddInterviewAsync(client, appId, Now.AddHours(24), "Other");

        Assert.Equal(new[] { "Prepare" }, await NudgeTypesAsync(client));

        // Completing the checklist clears the nudge.
        foreach (var item in new[] { "research-company", "questions-to-ask", "setup" })
            await OkAsync(await client.PutAsJsonAsync($"/api/jobapplications/{appId}/interviews/{interviewId}/checklist", new { itemId = item, done = true }));
        Assert.Empty(await NudgeTypesAsync(client));

        // The interview passes without a debrief.
        _factory.Time.Advance(TimeSpan.FromHours(30));
        Assert.Equal(new[] { "WriteDebrief" }, await NudgeTypesAsync(client));

        // Saving the debrief clears it.
        await OkAsync(await client.PutAsJsonAsync($"/api/jobapplications/{appId}/interviews/{interviewId}/debrief",
            new { rating = 3, rawNotes = "ok" }));
        Assert.Empty(await NudgeTypesAsync(client));
    }

    // ---------- insights ----------

    [DatabaseFact] // AC-6.3
    public async Task Insights_for_a_new_user_render_without_errors()
    {
        var client = await _factory.CreateSignedInClientAsync();

        var insights = await OkAsync(await client.GetAsync("/api/analytics/insights"));

        Assert.Equal(0, insights.GetProperty("totalApplications").GetInt32());
        Assert.Equal(JsonValueKind.Null, insights.GetProperty("interviewRatePercent").ValueKind);
        Assert.All(insights.GetProperty("funnel").EnumerateArray(), s => Assert.Equal(0, s.GetProperty("count").GetInt32()));
    }

    [DatabaseFact] // AC-6.1
    public async Task Insights_count_stages_from_the_timeline()
    {
        var client = await _factory.CreateSignedInClientAsync();
        var a = await CreateApplicationAsync(client, "A", "Applied", "LinkedIn");
        var b = await CreateApplicationAsync(client, "B", "Applied", "LinkedIn");
        await CreateApplicationAsync(client, "C", "Backlog");
        _factory.Time.Advance(TimeSpan.FromDays(4));
        await OkAsync(await client.PutAsJsonAsync($"/api/jobapplications/{a}/status", new { status = "Interviewing" }));
        _factory.Time.Advance(TimeSpan.FromDays(2));
        await OkAsync(await client.PutAsJsonAsync($"/api/jobapplications/{a}/status", new { status = "Rejected", rejectionReason = "Skills gap" }));

        var insights = await OkAsync(await client.GetAsync("/api/analytics/insights"));

        Assert.Equal(3, insights.GetProperty("totalApplications").GetInt32());
        Assert.Equal(2, insights.GetProperty("appliedApplications").GetInt32());
        var funnel = insights.GetProperty("funnel").EnumerateArray().ToDictionary(s => s.GetProperty("stage").GetString()!);
        Assert.Equal(1, funnel["Interviewing"].GetProperty("count").GetInt32()); // A is Rejected now but still counts
        Assert.Equal(50.0, funnel["Interviewing"].GetProperty("percent").GetDouble());
        Assert.Equal(4.0, insights.GetProperty("averageDaysToFirstReply").GetDouble());
        Assert.Equal("Skills gap", insights.GetProperty("rejectionReasons")[0].GetProperty("reason").GetString());
        Assert.False(insights.GetProperty("sources")[0].GetProperty("enoughData").GetBoolean());
        _ = b;
    }

    // ---------- offers and rates ----------

    [DatabaseFact] // AC-7.1, AC-7.2
    public async Task Offers_are_normalised_with_the_saved_rates_and_change_when_rates_change()
    {
        var client = await _factory.CreateSignedInClientAsync();
        var appId = await CreateApplicationAsync(client, "Brightwave", "Offer");

        var rates = await OkAsync(await client.GetAsync("/api/settings/rates"));
        Assert.Equal(7.64m, rates.GetProperty("usdToGtq").GetDecimal());

        var offer = await OkAsync(await client.PostAsJsonAsync($"/api/jobapplications/{appId}/offers",
            new { amount = 38400, currency = "EUR", period = "Annual", notes = "Health insurance" }));
        Assert.Equal(27328.00m, offer.GetProperty("monthlyGtq").GetDecimal());
        Assert.Equal(3576.96m, offer.GetProperty("monthlyUsd").GetDecimal());

        await OkAsync(await client.PutAsJsonAsync("/api/settings/rates", new { usdToGtq = 8, eurToGtq = 9 }));
        var comparison = await OkAsync(await client.GetAsync("/api/offers"));

        Assert.Equal(8m, comparison.GetProperty("rates").GetProperty("usdToGtq").GetDecimal());
        var row = Assert.Single(comparison.GetProperty("offers").EnumerateArray());
        Assert.Equal(28800.00m, row.GetProperty("monthlyGtq").GetDecimal()); // 3200 EUR x 9
        Assert.Equal(3600.00m, row.GetProperty("monthlyUsd").GetDecimal());
        Assert.Equal("Brightwave", row.GetProperty("companyName").GetString());
    }

    [DatabaseFact]
    public async Task Offer_and_rate_validation_returns_422()
    {
        var client = await _factory.CreateSignedInClientAsync();
        var appId = await CreateApplicationAsync(client, "Acme", "Offer");

        var zero = await client.PostAsJsonAsync($"/api/jobapplications/{appId}/offers", new { amount = 0, currency = "USD", period = "Monthly" });
        var badCurrency = await client.PostAsJsonAsync($"/api/jobapplications/{appId}/offers", new { amount = 100, currency = "JPY", period = "Monthly" });
        var tinyRate = await client.PutAsJsonAsync("/api/settings/rates", new { usdToGtq = 0.5, eurToGtq = 9 });
        var hugeRate = await client.PutAsJsonAsync("/api/settings/rates", new { usdToGtq = 76, eurToGtq = 9 });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, zero.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, badCurrency.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, tinyRate.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, hugeRate.StatusCode);
    }

    [DatabaseFact] // AC-7.5
    public async Task Suggested_rates_never_change_the_saved_rates()
    {
        var client = await _factory.CreateSignedInClientAsync();

        var suggested = await OkAsync(await client.GetAsync("/api/settings/rates/suggested"));
        var saved = await OkAsync(await client.GetAsync("/api/settings/rates"));

        Assert.Equal(7.6448m, suggested.GetProperty("usdToGtq").GetDecimal());
        Assert.Equal("ExchangeRate-API", suggested.GetProperty("provider").GetString());
        Assert.StartsWith("https://", suggested.GetProperty("attributionUrl").GetString());
        Assert.Equal(7.64m, saved.GetProperty("usdToGtq").GetDecimal());
    }

    [DatabaseFact] // AC-7.5, AC-AI-3 style failure handling
    public async Task When_the_rate_provider_fails_the_manual_flow_still_works()
    {
        var client = await _factory.CreateSignedInClientAsync();
        _factory.RateProvider = new FailingRateProvider();

        var suggested = await client.GetAsync("/api/settings/rates/suggested");
        var manual = await client.PutAsJsonAsync("/api/settings/rates", new { usdToGtq = 7.7, eurToGtq = 8.6 });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, suggested.StatusCode);
        Assert.Equal(HttpStatusCode.OK, manual.StatusCode);
    }

    // ---------- stories ----------

    [DatabaseFact]
    public async Task Stories_can_be_created_updated_and_deleted()
    {
        var client = await _factory.CreateSignedInClientAsync();
        var story = new { title = "Rate limiter", situation = "S", task = "T", action = "A", result = "R", tags = new[] { "backend", "Backend", " api " } };

        var created = await OkAsync(await client.PostAsJsonAsync("/api/stories", story));
        var id = created.GetProperty("id").GetGuid();
        Assert.Equal(new[] { "backend", "api" }, created.GetProperty("tags").EnumerateArray().Select(t => t.GetString()));

        var updated = await OkAsync(await client.PutAsJsonAsync($"/api/stories/{id}", new { title = "Rate limiter v2", situation = "S", task = "T", action = "A", result = "R", tags = Array.Empty<string>() }));
        Assert.Equal("Rate limiter v2", updated.GetProperty("title").GetString());

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/stories/{id}")).StatusCode);
        Assert.Empty(await ListAsync(client, "/api/stories"));
    }

    [DatabaseFact]
    public async Task Stories_require_every_star_field()
    {
        var client = await _factory.CreateSignedInClientAsync();

        var response = await client.PostAsJsonAsync("/api/stories", new { title = "Missing parts", situation = "S" });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // ---------- isolation for the new endpoints (AC-9.1) ----------

    [DatabaseFact]
    public async Task User_B_cannot_touch_user_A_interviews_offers_stories_or_timeline()
    {
        var a = await _factory.CreateSignedInClientAsync();
        var b = await _factory.CreateSignedInClientAsync();
        var appId = await CreateApplicationAsync(a, "Private", "Offer");
        var interviewId = await AddInterviewAsync(a, appId, Now.AddDays(-1));
        var offer = await OkAsync(await a.PostAsJsonAsync($"/api/jobapplications/{appId}/offers", new { amount = 1000, currency = "USD", period = "Monthly" }));
        var offerId = offer.GetProperty("id").GetGuid();
        var story = await OkAsync(await a.PostAsJsonAsync("/api/stories", new { title = "T", situation = "S", task = "T", action = "A", result = "R" }));
        var storyId = story.GetProperty("id").GetGuid();

        var attempts = new Dictionary<string, HttpResponseMessage>
        {
            ["get application"] = await b.GetAsync($"/api/jobapplications/{appId}"),
            ["timeline"] = await b.GetAsync($"/api/jobapplications/{appId}/timeline"),
            ["follow-up"] = await b.PostAsJsonAsync($"/api/jobapplications/{appId}/follow-ups", new { }),
            ["update interview"] = await b.PutAsJsonAsync($"/api/jobapplications/{appId}/interviews/{interviewId}", new { status = "Cancelled" }),
            ["checklist"] = await b.PutAsJsonAsync($"/api/jobapplications/{appId}/interviews/{interviewId}/checklist", new { itemId = "setup", done = true }),
            ["debrief"] = await b.PutAsJsonAsync($"/api/jobapplications/{appId}/interviews/{interviewId}/debrief", new { rating = 3, rawNotes = "x" }),
            ["add offer"] = await b.PostAsJsonAsync($"/api/jobapplications/{appId}/offers", new { amount = 5, currency = "USD", period = "Monthly" }),
            ["delete offer"] = await b.DeleteAsync($"/api/offers/{offerId}"),
            ["update story"] = await b.PutAsJsonAsync($"/api/stories/{storyId}", new { title = "x", situation = "S", task = "T", action = "A", result = "R" }),
            ["delete story"] = await b.DeleteAsync($"/api/stories/{storyId}"),
        };

        foreach (var (name, response) in attempts)
            Assert.True(response.StatusCode == HttpStatusCode.NotFound, $"{name}: expected 404 but got {(int)response.StatusCode}");

        // B's own views are empty; A's data is intact.
        Assert.Empty(await ListAsync(b, "/api/stories"));
        Assert.Empty((await OkAsync(await b.GetAsync("/api/offers"))).GetProperty("offers").EnumerateArray());
        Assert.Empty(await NudgeTypesAsync(b));
        Assert.Single((await OkAsync(await a.GetAsync("/api/offers"))).GetProperty("offers").EnumerateArray());
        Assert.Single(await ListAsync(a, "/api/stories"));
    }

    [DatabaseFact]
    public async Task New_endpoints_require_authentication()
    {
        var anonymous = _factory.CreateBrowserClient();

        foreach (var url in new[] { "/api/today", "/api/offers", "/api/stories", "/api/settings/rates", "/api/settings/rates/suggested", "/api/analytics/insights" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync(url)).StatusCode);
    }
}
