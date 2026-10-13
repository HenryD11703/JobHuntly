using System.ComponentModel.DataAnnotations;
using JobHuntly.Domain.Entities;

namespace JobHuntly.Application.DTOs;

public class ChecklistItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public bool Done { get; set; }
}

public class InterviewDto
{
    public Guid Id { get; set; }
    public Guid JobApplicationId { get; set; }
    public DateTime InterviewDate { get; set; }
    public InterviewRound RoundType { get; set; }
    public InterviewStatus Status { get; set; }
    public string? Notes { get; set; }
    public List<ChecklistItemDto> Checklist { get; set; } = new();
    public bool ChecklistComplete { get; set; }
    public DebriefDto? Debrief { get; set; }
}

public class CreateInterviewDto
{
    public DateTime InterviewDate { get; set; }

    [EnumDataType(typeof(InterviewRound))]
    public InterviewRound RoundType { get; set; } = InterviewRound.Other;

    [StringLength(5000)]
    public string? Notes { get; set; }
}

public class UpdateInterviewDto
{
    public DateTime? InterviewDate { get; set; }

    [EnumDataType(typeof(InterviewStatus))]
    public InterviewStatus? Status { get; set; }

    [StringLength(5000)]
    public string? Notes { get; set; }
}

public class ToggleChecklistItemDto
{
    [Required, StringLength(60)]
    public string ItemId { get; set; } = string.Empty;

    public bool Done { get; set; }
}

public class DebriefDto
{
    public int Rating { get; set; }
    public string RawNotes { get; set; } = string.Empty;
    public List<string> QuestionsAsked { get; set; } = new();
    public List<string> WentWell { get; set; } = new();
    public List<string> WentPoorly { get; set; } = new();
    public List<string> FollowUps { get; set; } = new();
    public DateTime SavedAt { get; set; }
}

public class SaveDebriefDto
{
    [Range(1, 5)]
    public int Rating { get; set; }

    [StringLength(10000)]
    public string RawNotes { get; set; } = string.Empty;

    [MaxLength(30)]
    public List<string> QuestionsAsked { get; set; } = new();

    [MaxLength(30)]
    public List<string> WentWell { get; set; } = new();

    [MaxLength(30)]
    public List<string> WentPoorly { get; set; } = new();

    [MaxLength(30)]
    public List<string> FollowUps { get; set; } = new();
}
