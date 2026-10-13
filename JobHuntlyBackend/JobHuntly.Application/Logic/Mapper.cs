using JobHuntly.Application.DTOs;
using JobHuntly.Domain.Entities;

namespace JobHuntly.Application.Logic;

public static class Mapper
{
    public static JobApplicationDto ToDto(JobApplication a) => new()
    {
        Id = a.Id,
        CompanyName = a.CompanyName,
        PositionTitle = a.PositionTitle,
        Status = a.Status,
        AppliedDate = a.AppliedDate,
        Notes = a.Notes,
        SalaryRange = a.SalaryRange,
        JobPostUrl = a.JobPostUrl,
        JobPostText = a.JobPostText,
        RejectionReason = a.RejectionReason,
        Source = a.Source,
        Location = a.Location,
        IsRemote = a.IsRemote,
        Interviews = a.Interviews.OrderBy(i => i.InterviewDate).Select(ToDto).ToList()
    };

    public static InterviewDto ToDto(Interview i)
    {
        var done = i.ChecklistDone.ToHashSet();
        return new InterviewDto
        {
            Id = i.Id,
            JobApplicationId = i.JobApplicationId,
            InterviewDate = i.InterviewDate,
            RoundType = i.RoundType,
            Status = i.Status,
            Notes = i.Notes,
            Checklist = ChecklistCatalog.For(i.RoundType)
                .Select(c => new ChecklistItemDto { Id = c.Id, Text = c.Text, Done = done.Contains(c.Id) })
                .ToList(),
            ChecklistComplete = ChecklistCatalog.IsComplete(i.RoundType, i.ChecklistDone),
            Debrief = i.Debrief is null ? null : ToDto(i.Debrief)
        };
    }

    public static DebriefDto ToDto(Debrief d) => new()
    {
        Rating = d.Rating,
        RawNotes = d.RawNotes,
        QuestionsAsked = d.QuestionsAsked.ToList(),
        WentWell = d.WentWell.ToList(),
        WentPoorly = d.WentPoorly.ToList(),
        FollowUps = d.FollowUps.ToList(),
        SavedAt = d.SavedAt
    };

    public static ApplicationEventDto ToDto(ApplicationEvent e) => new()
    {
        Id = e.Id,
        Type = e.Type,
        OccurredAt = e.OccurredAt,
        FromStatus = e.FromStatus,
        ToStatus = e.ToStatus,
        Note = e.Note
    };

    public static StoryDto ToDto(Story s) => new()
    {
        Id = s.Id,
        Title = s.Title,
        Situation = s.Situation,
        Task = s.Task,
        Action = s.Action,
        Result = s.Result,
        Tags = s.Tags.ToList(),
        CreatedAt = s.CreatedAt
    };

    public static OfferDto ToDto(Offer o, ExchangeRates rates)
    {
        var normalized = OfferComparator.Normalize(o.Amount, o.Currency, o.Period, rates);
        return new OfferDto
        {
            Id = o.Id,
            JobApplicationId = o.JobApplicationId,
            CompanyName = o.JobApplication.CompanyName,
            PositionTitle = o.JobApplication.PositionTitle,
            Amount = o.Amount,
            Currency = o.Currency,
            Period = o.Period,
            Notes = o.Notes,
            MonthlyUsd = normalized.MonthlyUsd,
            MonthlyGtq = normalized.MonthlyGtq
        };
    }
}
