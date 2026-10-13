using System.ComponentModel.DataAnnotations;

namespace JobHuntly.Application.DTOs;

public class ExtractJobPostRequest
{
    [Required, StringLength(20000, MinimumLength = 30)]
    public string Text { get; set; } = string.Empty;
}

/// <summary>A draft, never saved automatically: the user reviews it and confirms.</summary>
public class JobPostDraftDto
{
    public string? CompanyName { get; set; }
    public string? PositionTitle { get; set; }
    public string? SalaryRange { get; set; }
    public string? Location { get; set; }
    public bool IsRemote { get; set; }
    public List<string> Requirements { get; set; } = new();
}

public class PrepRequest
{
    /// <summary>Ask for a fresh set even if one is already saved (this uses one AI call).</summary>
    public bool Regenerate { get; set; }
}

public class PrepQuestionDto
{
    public string Question { get; set; } = string.Empty;
    public Guid? StoryId { get; set; }
    public string? StoryTitle { get; set; }
}

public class PrepQuestionsDto
{
    public DateTime GeneratedAt { get; set; }
    public List<PrepQuestionDto> Questions { get; set; } = new();
}

public class PrepResultDto
{
    public PrepQuestionsDto Prep { get; set; } = new();

    /// <summary>True when the saved set was returned and no AI call (or quota) was used.</summary>
    public bool FromCache { get; set; }
}

public class StructureDebriefRequest
{
    [Required, StringLength(10000, MinimumLength = 10)]
    public string RawNotes { get; set; } = string.Empty;
}

/// <summary>A draft of the structured debrief. The user edits it and saves it with the debrief endpoint.</summary>
public class DebriefDraftDto
{
    public List<string> QuestionsAsked { get; set; } = new();
    public List<string> WentWell { get; set; } = new();
    public List<string> WentPoorly { get; set; } = new();
    public List<string> FollowUps { get; set; } = new();
}

public class AiStatusDto
{
    /// <summary>False when the server has no Groq key: the UI hides the AI buttons.</summary>
    public bool Enabled { get; set; }

    public int DailyLimit { get; set; }
    public int UsedToday { get; set; }
    public DateTime ResetsAt { get; set; }
    public string Provider { get; set; } = "Groq";
}
