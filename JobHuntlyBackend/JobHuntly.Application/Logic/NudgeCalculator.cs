using JobHuntly.Domain.Entities;

namespace JobHuntly.Application.Logic;

public enum NudgeType
{
    Prepare = 0,
    WriteDebrief = 1,
    FollowUp = 2
}

public sealed record Nudge(
    NudgeType Type,
    Guid ApplicationId,
    Guid? InterviewId,
    string CompanyName,
    string PositionTitle,
    string Reason,
    DateTime Since);

/// <summary>
/// Computes the "Today" list from plain rules. No AI, no randomness: time is passed in so the
/// boundaries are testable.
/// </summary>
public static class NudgeCalculator
{
    public static readonly TimeSpan FollowUpAfter = TimeSpan.FromDays(7);
    public static readonly TimeSpan PrepareWindow = TimeSpan.FromHours(48);

    /// <param name="applications">Applications with Events, Interviews and their Debrief loaded.</param>
    public static IReadOnlyList<Nudge> Calculate(IEnumerable<JobApplication> applications, DateTime nowUtc)
    {
        var nudges = new List<Nudge>();

        foreach (var app in applications)
        {
            // Prepare: a scheduled interview in the next 48 h whose checklist is not complete.
            if (app.Status != ApplicationStatus.Rejected)
            {
                foreach (var interview in app.Interviews)
                {
                    var inWindow = interview.InterviewDate >= nowUtc && interview.InterviewDate <= nowUtc + PrepareWindow;
                    if (interview.Status == InterviewStatus.Scheduled && inWindow
                        && !ChecklistCatalog.IsComplete(interview.RoundType, interview.ChecklistDone))
                    {
                        nudges.Add(new Nudge(NudgeType.Prepare, app.Id, interview.Id, app.CompanyName, app.PositionTitle,
                            $"{interview.RoundType} round is coming up and the checklist is not complete.",
                            interview.InterviewDate));
                    }
                }
            }

            // Write a debrief: the interview already happened (or was marked done) and nothing was recorded.
            foreach (var interview in app.Interviews)
            {
                if (interview.Status != InterviewStatus.Cancelled && interview.InterviewDate < nowUtc && interview.Debrief is null)
                {
                    nudges.Add(new Nudge(NudgeType.WriteDebrief, app.Id, interview.Id, app.CompanyName, app.PositionTitle,
                        $"The {interview.RoundType} round has passed and has no debrief yet.",
                        interview.InterviewDate));
                }
            }

            // Follow up: applied, and nothing at all has happened for 7 days or more.
            if (app.Status == ApplicationStatus.Applied)
            {
                var lastActivity = app.Events.Count > 0 ? app.Events.Max(e => e.OccurredAt) : app.AppliedDate;
                if (nowUtc - lastActivity >= FollowUpAfter)
                {
                    var days = (int)Math.Floor((nowUtc - lastActivity).TotalDays);
                    nudges.Add(new Nudge(NudgeType.FollowUp, app.Id, null, app.CompanyName, app.PositionTitle,
                        $"No activity for {days} days since you applied.", lastActivity));
                }
            }
        }

        return nudges
            .OrderBy(n => n.Type)
            .ThenBy(n => n.Since)
            .ToList();
    }
}
