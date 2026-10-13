using JobHuntly.Application.DTOs;
using JobHuntly.Domain.Entities;

namespace JobHuntly.Application.Logic;

/// <summary>
/// Funnel and timing numbers computed from each application's timeline events (not only from the
/// current status), so an application that ended in Rejected still counts for every stage it reached.
///
/// Definitions (also covered by tests):
///  - Applied reached: the timeline contains Applied, Interviewing, TechnicalTest or Offer.
///    Backlog-only and Backlog-then-Rejected applications are not counted as applied.
///  - Interviewing reached: Interviewing, TechnicalTest or Offer in the timeline, or any interview logged.
///  - Replied: applied, and then reached Interviewing or ended Rejected.
/// </summary>
public static class InsightsCalculator
{
    public const int MinGroupSize = 5;

    private static readonly ApplicationStatus[] AppliedOrLater =
        { ApplicationStatus.Applied, ApplicationStatus.Interviewing, ApplicationStatus.TechnicalTest, ApplicationStatus.Offer };

    private static readonly ApplicationStatus[] InterviewingOrLater =
        { ApplicationStatus.Interviewing, ApplicationStatus.TechnicalTest, ApplicationStatus.Offer };

    public static InsightsDto Calculate(IReadOnlyCollection<JobApplication> applications)
    {
        var result = new InsightsDto { MinGroupSize = MinGroupSize, TotalApplications = applications.Count };

        var infos = applications.Select(Describe).ToList();
        var applied = infos.Where(i => i.ReachedApplied).ToList();
        result.AppliedApplications = applied.Count;

        // Funnel
        result.Funnel = new List<FunnelStageDto>
        {
            Stage("Applied", applied.Count, applied.Count),
            Stage("Interviewing", applied.Count(i => i.ReachedInterviewing), applied.Count),
            Stage("TechnicalTest", applied.Count(i => i.Reached.Contains(ApplicationStatus.TechnicalTest)), applied.Count),
            Stage("Offer", applied.Count(i => i.Reached.Contains(ApplicationStatus.Offer)), applied.Count)
        };
        result.InterviewRatePercent = Percent(applied.Count(i => i.ReachedInterviewing), applied.Count);

        // Reply rate by source (only meaningful with enough applications in the group)
        result.Sources = applied
            .GroupBy(i => i.App.Source ?? ApplicationSource.Other)
            .Select(g =>
            {
                var count = g.Count();
                var replies = g.Count(i => i.Replied);
                var enough = count >= MinGroupSize;
                return new SourceReplyDto
                {
                    Source = g.Key.ToString(),
                    Applications = count,
                    Replies = replies,
                    EnoughData = enough,
                    ReplyRatePercent = enough ? Percent(replies, count) : null
                };
            })
            .OrderByDescending(s => s.Applications)
            .ThenBy(s => s.Source, StringComparer.Ordinal)
            .ToList();

        // Time to first reply
        var waits = applied
            .Select(i => i.DaysToFirstReply)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();
        result.AverageDaysToFirstReply = waits.Count == 0 ? null : Round1(waits.Average());

        // Time in each stage (completed stays only: the stage was left)
        result.StageDurations = new[] { ApplicationStatus.Applied, ApplicationStatus.Interviewing, ApplicationStatus.TechnicalTest }
            .Select(stage =>
            {
                var samples = infos.SelectMany(i => i.StayDays.Where(s => s.Stage == stage).Select(s => s.Days)).ToList();
                return new StageDurationDto
                {
                    Stage = stage.ToString(),
                    Samples = samples.Count,
                    AverageDays = samples.Count == 0 ? null : Round1(samples.Average())
                };
            })
            .ToList();

        // Rejection reasons
        result.RejectionReasons = applications
            .Where(a => a.Status == ApplicationStatus.Rejected)
            .Select(a => string.IsNullOrWhiteSpace(a.RejectionReason) ? "No reason given" : a.RejectionReason.Trim())
            .GroupBy(r => r, StringComparer.OrdinalIgnoreCase)
            .Select(g => new RejectionReasonDto { Reason = g.Key, Count = g.Count() })
            .OrderByDescending(r => r.Count)
            .ThenBy(r => r.Reason, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return result;
    }

    private sealed record Stay(ApplicationStatus Stage, double Days);

    private sealed record AppInfo(
        JobApplication App,
        HashSet<ApplicationStatus> Reached,
        bool ReachedApplied,
        bool ReachedInterviewing,
        bool Replied,
        double? DaysToFirstReply,
        List<Stay> StayDays);

    private static AppInfo Describe(JobApplication app)
    {
        var statusEvents = app.Events
            .Where(e => e.ToStatus.HasValue)
            .OrderBy(e => e.OccurredAt).ThenBy(e => e.Type)
            .ToList();

        var reached = statusEvents.Select(e => e.ToStatus!.Value).ToHashSet();
        reached.Add(app.Status); // safety net for data written without events

        var reachedApplied = AppliedOrLater.Any(reached.Contains);
        var hasInterviewEvent = app.Events.Any(e => e.Type == EventType.InterviewAdded) || app.Interviews.Count > 0;
        var reachedInterviewing = reachedApplied && (InterviewingOrLater.Any(reached.Contains) || hasInterviewEvent);
        var replied = reachedApplied && (reachedInterviewing || reached.Contains(ApplicationStatus.Rejected));

        // First reply: first event after Applied that shows the company answered.
        double? daysToFirstReply = null;
        var appliedEvent = statusEvents.FirstOrDefault(e => e.ToStatus == ApplicationStatus.Applied);
        if (appliedEvent is not null)
        {
            var reply = app.Events
                .Where(e => e.OccurredAt > appliedEvent.OccurredAt)
                .OrderBy(e => e.OccurredAt)
                .FirstOrDefault(e => e.Type == EventType.InterviewAdded
                                     || e.ToStatus is ApplicationStatus.Interviewing or ApplicationStatus.TechnicalTest
                                         or ApplicationStatus.Offer or ApplicationStatus.Rejected);
            if (reply is not null)
                daysToFirstReply = (reply.OccurredAt - appliedEvent.OccurredAt).TotalDays;
        }

        var stays = new List<Stay>();
        for (var i = 0; i < statusEvents.Count - 1; i++)
        {
            var days = (statusEvents[i + 1].OccurredAt - statusEvents[i].OccurredAt).TotalDays;
            stays.Add(new Stay(statusEvents[i].ToStatus!.Value, days));
        }

        return new AppInfo(app, reached, reachedApplied, reachedInterviewing, replied, daysToFirstReply, stays);
    }

    private static FunnelStageDto Stage(string name, int count, int denominator) =>
        new() { Stage = name, Count = count, Percent = Percent(count, denominator) };

    private static double? Percent(int part, int whole) =>
        whole == 0 ? null : Round1(part * 100.0 / whole);

    private static double Round1(double value) => Math.Round(value, 1, MidpointRounding.AwayFromZero);
}
