using JobHuntly.Application.Logic;
using JobHuntly.Domain.Entities;
using static JobHuntly.Tests.Unit.Builders;

namespace JobHuntly.Tests.Unit;

/// <summary>AC-6.1 to AC-6.3.</summary>
public class InsightsCalculatorTests
{
    private static JobApplication Journey(ApplicationSource? source, params (ApplicationStatus To, int DaysAgo)[] steps)
    {
        var app = App(steps[^1].To, source: source);
        foreach (var (to, daysAgo) in steps) app.Moved(to, Now.AddDays(-daysAgo));
        return app;
    }

    [Fact]
    public void Empty_state_has_no_nan_and_no_percentages()
    {
        var result = InsightsCalculator.Calculate(Array.Empty<JobApplication>());

        Assert.Equal(0, result.TotalApplications);
        Assert.All(result.Funnel, s => { Assert.Equal(0, s.Count); Assert.Null(s.Percent); });
        Assert.Null(result.InterviewRatePercent);
        Assert.Null(result.AverageDaysToFirstReply);
        Assert.Empty(result.Sources);
        Assert.Empty(result.RejectionReasons);
        Assert.All(result.StageDurations, d => Assert.Null(d.AverageDays));
    }

    [Fact]
    public void Funnel_uses_the_timeline_so_a_rejected_application_still_counts_for_stages_it_reached()
    {
        var apps = new[]
        {
            Journey(null, (ApplicationStatus.Applied, 20), (ApplicationStatus.Interviewing, 15), (ApplicationStatus.Rejected, 5)),
            Journey(null, (ApplicationStatus.Applied, 20), (ApplicationStatus.Interviewing, 12), (ApplicationStatus.TechnicalTest, 8), (ApplicationStatus.Offer, 2)),
            Journey(null, (ApplicationStatus.Applied, 10)),
            Journey(null, (ApplicationStatus.Backlog, 3)),
        };

        var result = InsightsCalculator.Calculate(apps);

        Assert.Equal(4, result.TotalApplications);
        Assert.Equal(3, result.AppliedApplications); // the Backlog one is not counted
        var funnel = result.Funnel.ToDictionary(s => s.Stage);
        Assert.Equal(3, funnel["Applied"].Count);
        Assert.Equal(2, funnel["Interviewing"].Count);   // includes the rejected-after-interview one
        Assert.Equal(1, funnel["TechnicalTest"].Count);
        Assert.Equal(1, funnel["Offer"].Count);
        Assert.Equal(66.7, funnel["Interviewing"].Percent);
        Assert.Equal(66.7, result.InterviewRatePercent);
    }

    [Fact]
    public void An_interview_logged_counts_as_reaching_interviewing_even_without_a_status_change()
    {
        var app = Journey(null, (ApplicationStatus.Applied, 10));
        app.AddInterview(Now.AddDays(2));

        var result = InsightsCalculator.Calculate(new[] { app });

        Assert.Equal(1, result.Funnel.Single(s => s.Stage == "Interviewing").Count);
    }

    [Fact]
    public void Backlog_then_rejected_is_not_counted_as_applied()
    {
        var app = Journey(null, (ApplicationStatus.Backlog, 10), (ApplicationStatus.Rejected, 5));

        var result = InsightsCalculator.Calculate(new[] { app });

        Assert.Equal(0, result.AppliedApplications);
    }

    [Fact]
    public void Reply_rate_by_source_hides_percentages_for_groups_under_five()
    {
        var apps = new List<JobApplication>();
        // LinkedIn: 5 applications, 2 replies (one interview, one rejection)
        apps.Add(Journey(ApplicationSource.LinkedIn, (ApplicationStatus.Applied, 20), (ApplicationStatus.Interviewing, 10)));
        apps.Add(Journey(ApplicationSource.LinkedIn, (ApplicationStatus.Applied, 20), (ApplicationStatus.Rejected, 10)));
        for (var i = 0; i < 3; i++) apps.Add(Journey(ApplicationSource.LinkedIn, (ApplicationStatus.Applied, 5)));
        // Referral: 4 applications -> not enough data
        for (var i = 0; i < 4; i++) apps.Add(Journey(ApplicationSource.Referral, (ApplicationStatus.Applied, 5)));

        var sources = InsightsCalculator.Calculate(apps).Sources.ToDictionary(s => s.Source);

        Assert.True(sources["LinkedIn"].EnoughData);
        Assert.Equal(40.0, sources["LinkedIn"].ReplyRatePercent);
        Assert.Equal(2, sources["LinkedIn"].Replies);
        Assert.False(sources["Referral"].EnoughData);
        Assert.Null(sources["Referral"].ReplyRatePercent);
        Assert.Equal(4, sources["Referral"].Applications);
    }

    [Fact]
    public void Applications_without_a_source_are_grouped_as_Other()
    {
        var result = InsightsCalculator.Calculate(new[] { Journey(null, (ApplicationStatus.Applied, 3)) });

        Assert.Equal("Other", Assert.Single(result.Sources).Source);
    }

    [Fact]
    public void Time_to_first_reply_and_time_in_stage_use_completed_stays()
    {
        // Applied 20 days ago, first reply (Interviewing) 14 days ago -> 6 days; Interviewing for 4 days.
        var a = Journey(null, (ApplicationStatus.Applied, 20), (ApplicationStatus.Interviewing, 14), (ApplicationStatus.Rejected, 10));
        // Applied 10 days ago, rejected 5 days ago -> 5 days.
        var b = Journey(null, (ApplicationStatus.Applied, 10), (ApplicationStatus.Rejected, 5));

        var result = InsightsCalculator.Calculate(new[] { a, b });

        Assert.Equal(5.5, result.AverageDaysToFirstReply);
        var applied = result.StageDurations.Single(d => d.Stage == "Applied");
        Assert.Equal(2, applied.Samples);
        Assert.Equal(5.5, applied.AverageDays);  // (6 + 5) / 2
        var interviewing = result.StageDurations.Single(d => d.Stage == "Interviewing");
        Assert.Equal(4.0, interviewing.AverageDays);
    }

    [Fact]
    public void Rejection_reasons_are_grouped_ignoring_case_with_a_label_for_missing_ones()
    {
        var apps = new[]
        {
            Journey(null, (ApplicationStatus.Applied, 10), (ApplicationStatus.Rejected, 5)),
            Journey(null, (ApplicationStatus.Applied, 10), (ApplicationStatus.Rejected, 5)),
            Journey(null, (ApplicationStatus.Applied, 10), (ApplicationStatus.Rejected, 5)),
        };
        apps[0].RejectionReason = "Skills gap";
        apps[1].RejectionReason = "  skills GAP ";
        apps[2].RejectionReason = null;

        var reasons = InsightsCalculator.Calculate(apps).RejectionReasons;

        Assert.Equal(2, reasons.Count);
        Assert.Equal(2, reasons[0].Count);
        Assert.Equal("Skills gap", reasons[0].Reason);
        Assert.Equal("No reason given", reasons[1].Reason);
    }
}
