using JobHuntly.Application.Logic;
using JobHuntly.Domain.Entities;
using static JobHuntly.Tests.Unit.Builders;

namespace JobHuntly.Tests.Unit;

/// <summary>AC-5.1 and AC-5.3, including the boundary cases.</summary>
public class NudgeCalculatorTests
{
    private static IReadOnlyList<Nudge> Run(params JobApplication[] apps) => NudgeCalculator.Calculate(apps, Now);

    // ---- follow up ----

    [Fact]
    public void Follow_up_fires_at_exactly_seven_days()
    {
        var app = App().Moved(ApplicationStatus.Applied, Now.AddDays(-7));

        var nudge = Assert.Single(Run(app));
        Assert.Equal(NudgeType.FollowUp, nudge.Type);
    }

    [Fact]
    public void Follow_up_does_not_fire_one_second_before_seven_days()
    {
        var app = App().Moved(ApplicationStatus.Applied, Now.AddDays(-7).AddSeconds(1));

        Assert.Empty(Run(app));
    }

    [Fact]
    public void Six_days_23_hours_is_not_enough()
    {
        var app = App().Moved(ApplicationStatus.Applied, Now.AddDays(-6).AddHours(-23));

        Assert.Empty(Run(app));
    }

    [Fact]
    public void Logging_a_follow_up_resets_the_clock()
    {
        var app = App().Moved(ApplicationStatus.Applied, Now.AddDays(-10))
            .WithEvent(EventType.FollowedUp, Now.AddDays(-2));

        Assert.Empty(Run(app));
    }

    [Theory]
    [InlineData(ApplicationStatus.Rejected)]
    [InlineData(ApplicationStatus.Offer)]
    [InlineData(ApplicationStatus.Backlog)]
    [InlineData(ApplicationStatus.Interviewing)]
    [InlineData(ApplicationStatus.TechnicalTest)]
    public void Only_applications_in_Applied_get_follow_up_nudges(ApplicationStatus status)
    {
        var app = App(status).Moved(status, Now.AddDays(-30));

        Assert.DoesNotContain(Run(app), n => n.Type == NudgeType.FollowUp);
    }

    // ---- prepare ----

    [Fact]
    public void Prepare_fires_for_an_interview_in_the_next_48_hours_with_an_open_checklist()
    {
        var app = App(ApplicationStatus.Interviewing).Moved(ApplicationStatus.Interviewing, Now.AddDays(-3));
        app.AddInterview(Now.AddHours(30), InterviewRound.Technical, checklistDone: "reread-post");

        var nudge = Assert.Single(Run(app));
        Assert.Equal(NudgeType.Prepare, nudge.Type);
    }

    [Fact]
    public void Prepare_window_includes_exactly_48_hours_and_excludes_beyond()
    {
        var inside = App(ApplicationStatus.Interviewing).Moved(ApplicationStatus.Interviewing, Now.AddDays(-3));
        inside.AddInterview(Now.AddHours(48));
        var outside = App(ApplicationStatus.Interviewing).Moved(ApplicationStatus.Interviewing, Now.AddDays(-3));
        outside.AddInterview(Now.AddHours(48).AddSeconds(1));

        Assert.Single(Run(inside));
        Assert.Empty(Run(outside));
    }

    [Fact]
    public void Prepare_is_not_needed_when_the_checklist_is_complete()
    {
        var app = App(ApplicationStatus.Interviewing).Moved(ApplicationStatus.Interviewing, Now.AddDays(-3));
        var all = ChecklistCatalog.For(InterviewRound.Technical).Select(i => i.Id).ToArray();
        app.AddInterview(Now.AddHours(10), InterviewRound.Technical, checklistDone: all);

        Assert.Empty(Run(app));
    }

    [Fact]
    public void Prepare_ignores_cancelled_and_rejected()
    {
        var cancelled = App(ApplicationStatus.Interviewing).Moved(ApplicationStatus.Interviewing, Now.AddDays(-3));
        cancelled.AddInterview(Now.AddHours(10), status: InterviewStatus.Cancelled);
        var rejected = App(ApplicationStatus.Rejected).Moved(ApplicationStatus.Rejected, Now.AddDays(-1));
        rejected.AddInterview(Now.AddHours(10));

        Assert.DoesNotContain(Run(cancelled, rejected), n => n.Type == NudgeType.Prepare);
    }

    // ---- debrief ----

    [Fact]
    public void Debrief_is_requested_for_a_past_interview_without_one()
    {
        var app = App(ApplicationStatus.Interviewing).Moved(ApplicationStatus.Interviewing, Now.AddDays(-5));
        app.AddInterview(Now.AddDays(-1), InterviewRound.HrScreen);

        var nudge = Assert.Single(Run(app));
        Assert.Equal(NudgeType.WriteDebrief, nudge.Type);
    }

    [Fact]
    public void Debrief_is_not_requested_when_one_exists_or_the_interview_is_cancelled_or_future()
    {
        var app = App(ApplicationStatus.Interviewing).Moved(ApplicationStatus.Interviewing, Now.AddDays(-5));
        app.AddInterview(Now.AddDays(-1), withDebrief: true, status: InterviewStatus.Done);
        app.AddInterview(Now.AddDays(-2), status: InterviewStatus.Cancelled);
        app.AddInterview(Now.AddDays(5)); // future, outside the prepare window

        Assert.Empty(Run(app));
    }

    // ---- ordering ----

    [Fact]
    public void Results_are_ordered_prepare_then_debrief_then_follow_up()
    {
        var follow = App().Moved(ApplicationStatus.Applied, Now.AddDays(-9));
        var debrief = App(ApplicationStatus.Interviewing).Moved(ApplicationStatus.Interviewing, Now.AddDays(-2));
        debrief.AddInterview(Now.AddDays(-1));
        var prepare = App(ApplicationStatus.Interviewing).Moved(ApplicationStatus.Interviewing, Now.AddDays(-2));
        prepare.AddInterview(Now.AddHours(5));

        var types = Run(follow, debrief, prepare).Select(n => n.Type).ToArray();

        Assert.Equal(new[] { NudgeType.Prepare, NudgeType.WriteDebrief, NudgeType.FollowUp }, types);
    }

    [Fact]
    public void No_applications_means_no_nudges()
    {
        Assert.Empty(Run());
    }
}
