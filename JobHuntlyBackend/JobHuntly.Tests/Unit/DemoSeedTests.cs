using JobHuntly.Application.Demo;
using JobHuntly.Application.Logic;
using JobHuntly.Domain.Entities;

namespace JobHuntly.Tests.Unit;

/// <summary>AC-8.1 and AC-8.4: what every demo sandbox starts with.</summary>
public class DemoSeedTests
{
    private static readonly DateTime Now = new(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);

    private static User Seed(DateTime? now = null) => DemoSeed.Build("demo-x@demo.jobhuntly.invalid", "hash", now ?? Now);

    [Fact] // AC-8.1
    public void The_sandbox_has_enough_to_explore_on_every_screen()
    {
        var user = Seed();
        var interviews = user.JobApplications.SelectMany(a => a.Interviews).ToList();

        Assert.True(user.IsDemo);
        Assert.True(user.JobApplications.Count >= 8, "at least 8 applications");
        Assert.True(interviews.Count >= 3, "at least 3 interviews");
        Assert.True(interviews.Count(i => i.Debrief is not null) >= 1, "at least one debrief");
        Assert.True(user.Stories.Count >= 2, "at least 2 stories");
        Assert.Equal(3, user.JobApplications.SelectMany(a => a.Offers).Count());

        // Every board column has something in it.
        var statuses = user.JobApplications.Select(a => a.Status).Distinct().ToHashSet();
        foreach (var status in Enum.GetValues<ApplicationStatus>())
            Assert.Contains(status, statuses);
    }

    [Fact]
    public void Today_shows_exactly_one_prepare_one_debrief_and_one_follow_up()
    {
        var user = Seed();

        var nudges = NudgeCalculator.Calculate(user.JobApplications, Now.AddSeconds(30));

        Assert.Equal(new[] { NudgeType.Prepare, NudgeType.WriteDebrief, NudgeType.FollowUp }, nudges.Select(n => n.Type));
        Assert.Equal("Northwind Labs", nudges[0].CompanyName);
        Assert.Equal("Cobalt Street", nudges[1].CompanyName);
        Assert.Equal("Tessel", nudges[2].CompanyName);
    }

    [Fact]
    public void Insights_have_one_source_with_enough_data_and_a_real_funnel()
    {
        var insights = InsightsCalculator.Calculate(Seed().JobApplications.ToList());

        Assert.Equal(14, insights.TotalApplications);
        Assert.Equal(12, insights.AppliedApplications);   // two are still in Backlog
        Assert.Single(insights.Sources, s => s.EnoughData);
        Assert.Equal("LinkedIn", insights.Sources.Single(s => s.EnoughData).Source);
        Assert.All(insights.Funnel, stage => Assert.True(stage.Count > 0));
        Assert.NotNull(insights.AverageDaysToFirstReply);
        Assert.Contains(insights.RejectionReasons, r => r.Reason == "Skills gap");
    }

    [Fact]
    public void The_three_offers_compare_in_three_currencies()
    {
        var user = Seed();
        var rates = new ExchangeRates(user.UsdToGtq, user.EurToGtq);

        var rows = user.JobApplications.SelectMany(a => a.Offers)
            .Select(o => OfferComparator.Normalize(o.Amount, o.Currency, o.Period, rates)).ToList();

        Assert.Equal(new[] { Currency.USD, Currency.EUR, Currency.GTQ }.OrderBy(c => c),
            user.JobApplications.SelectMany(a => a.Offers).Select(o => o.Currency).OrderBy(c => c));
        Assert.All(rows, r => Assert.True(r.MonthlyUsd > 1000m));
    }

    [Fact]
    public void Each_timeline_ends_in_the_applications_current_status()
    {
        foreach (var app in Seed().JobApplications)
        {
            var last = app.Events.Where(e => e.ToStatus.HasValue).OrderBy(e => e.OccurredAt).Last();
            Assert.True(last.ToStatus == app.Status, $"{app.CompanyName}: timeline ends in {last.ToStatus}, status is {app.Status}");
            Assert.Equal(EventType.Created, app.Events.OrderBy(e => e.OccurredAt).First().Type);
        }
    }

    [Fact]
    public void Nothing_is_in_the_future_except_the_upcoming_interview_and_all_ids_are_unique()
    {
        var user = Seed();
        var events = user.JobApplications.SelectMany(a => a.Events).ToList();

        Assert.All(events, e => Assert.True(e.OccurredAt <= Now));
        Assert.Single(user.JobApplications.SelectMany(a => a.Interviews), i => i.InterviewDate > Now);

        var ids = user.JobApplications.Select(a => a.Id)
            .Concat(events.Select(e => e.Id))
            .Concat(user.JobApplications.SelectMany(a => a.Interviews).Select(i => i.Id))
            .Concat(user.Stories.Select(s => s.Id)).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Fact] // AC-8.4
    public void Sample_data_is_fictional()
    {
        var user = Seed();

        Assert.All(user.JobApplications, a => Assert.Contains(a.CompanyName, DemoSeed.Companies));
        Assert.EndsWith(".invalid", user.Email);                        // .invalid can never be a real mailbox
        Assert.All(user.JobApplications, a => Assert.True(a.JobPostUrl is null));  // no links to real sites
        Assert.Equal("Demo", user.FirstName);                           // no real person
    }

    [Fact]
    public void The_seed_is_relative_to_now_so_every_sandbox_looks_current()
    {
        var nowEarlier = Now.AddYears(-1);

        List<(string Company, double DaysAgo)> Offsets(User user, DateTime now) =>
            user.JobApplications.OrderBy(a => a.CompanyName).Select(a => (a.CompanyName, (now - a.AppliedDate).TotalDays)).ToList();

        Assert.Equal(Offsets(Seed(nowEarlier), nowEarlier), Offsets(Seed(Now), Now));
    }

    [Theory]
    [InlineData(0)]    // just after midnight UTC
    [InlineData(7)]
    [InlineData(12)]
    [InlineData(19)]   // evening
    [InlineData(23)]
    public void Interviews_sit_at_a_sensible_hour_and_the_upcoming_one_is_inside_the_prepare_window(int hourNow)
    {
        var now = new DateTime(2026, 10, 13, hourNow, 17, 0, DateTimeKind.Utc);

        var interviews = Seed(now).JobApplications.SelectMany(a => a.Interviews).ToList();

        Assert.All(interviews, i => Assert.Equal(15, i.InterviewDate.Hour));   // 15:00 UTC = 09:00 in Guatemala
        var upcoming = Assert.Single(interviews, i => i.InterviewDate > now);
        Assert.InRange((upcoming.InterviewDate - now).TotalHours, 20, 44);
        Assert.Contains(NudgeCalculator.Calculate(Seed(now).JobApplications, now.AddMinutes(1)), n => n.Type == NudgeType.Prepare);
    }
}