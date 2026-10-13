using System.Text.Json;
using JobHuntly.Application.Logic;
using JobHuntly.Domain.Entities;
using JobHuntly.Infrastructure.Rates;

namespace JobHuntly.Tests.Unit;

public class ChecklistCatalogTests
{
    public static IEnumerable<object[]> Rounds() =>
        Enum.GetValues<InterviewRound>().Select(r => new object[] { r });

    [Theory, MemberData(nameof(Rounds))]
    public void Every_round_has_a_checklist_with_unique_ids(InterviewRound round)
    {
        var items = ChecklistCatalog.For(round);

        Assert.NotEmpty(items);
        Assert.Equal(items.Count, items.Select(i => i.Id).Distinct().Count());
        Assert.All(items, i => Assert.False(string.IsNullOrWhiteSpace(i.Text)));
    }

    [Fact]
    public void Technical_round_matches_the_approved_mockup()
    {
        var ids = ChecklistCatalog.For(InterviewRound.Technical).Select(i => i.Id).ToArray();

        Assert.Equal(new[] { "reread-post", "product", "setup", "questions-to-ask", "practice" }, ids);
    }

    [Fact]
    public void Contains_and_IsComplete_work_per_round()
    {
        Assert.True(ChecklistCatalog.Contains(InterviewRound.Technical, "practice"));
        Assert.False(ChecklistCatalog.Contains(InterviewRound.HrScreen, "practice"));
        Assert.False(ChecklistCatalog.IsComplete(InterviewRound.Other, Array.Empty<string>()));
        Assert.True(ChecklistCatalog.IsComplete(InterviewRound.Other,
            ChecklistCatalog.For(InterviewRound.Other).Select(i => i.Id)));
    }
}

/// <summary>AC-7.5 validation. The real call is checked manually (see the task log).</summary>
public class OpenErApiRateProviderTests
{
    private static JsonElement Json(string text) => JsonDocument.Parse(text).RootElement;

    [Fact]
    public void Parses_a_valid_payload_and_derives_eur_to_gtq()
    {
        var quote = OpenErApiRateProvider.Parse(Json("""
            {"result":"success","time_last_update_unix":1791849451,
             "rates":{"GTQ":7.64477,"EUR":0.895156}}
            """));

        Assert.Equal(7.6448m, quote.UsdToGtq);
        Assert.Equal(8.5402m, quote.EurToGtq);   // 7.64477 / 0.895156 = 8.540154
        Assert.Equal("ExchangeRate-API", quote.Provider);
        Assert.StartsWith("https://", quote.AttributionUrl);
        Assert.Equal(DateTimeKind.Utc, quote.ProviderUpdatedAtUtc.Kind);
    }

    [Theory]
    [InlineData("""{"result":"error","rates":{"GTQ":7.6,"EUR":0.9}}""")]
    [InlineData("""{"result":"success","rates":{"EUR":0.9}}""")]
    [InlineData("""{"result":"success","rates":{"GTQ":7.6}}""")]
    [InlineData("""{"result":"success","rates":{"GTQ":0.5,"EUR":0.9}}""")]
    [InlineData("""{"result":"success","rates":{"GTQ":76,"EUR":0.9}}""")]
    [InlineData("""{"result":"success","rates":{"GTQ":7.6,"EUR":0}}""")]
    [InlineData("""{"result":"success"}""")]
    public void Rejects_broken_or_implausible_payloads(string payload)
    {
        Assert.Throws<InvalidDataException>(() => OpenErApiRateProvider.Parse(Json(payload)));
    }
}

public class UtcDateTests
{
    [Fact]
    public void Unspecified_is_treated_as_utc_and_local_is_converted()
    {
        var unspecified = new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Unspecified);
        Assert.Equal(DateTimeKind.Utc, UtcDate.Normalize(unspecified).Kind);
        Assert.Equal(unspecified.Hour, UtcDate.Normalize(unspecified).Hour);

        var local = new DateTime(2026, 10, 15, 10, 0, 0, DateTimeKind.Local);
        Assert.Equal(local.ToUniversalTime(), UtcDate.Normalize(local));
    }
}
