namespace JobHuntly.Application.DTOs;

public class InsightsDto
{
    /// <summary>Groups smaller than this show "not enough data" instead of a percentage.</summary>
    public int MinGroupSize { get; set; }

    public int TotalApplications { get; set; }

    /// <summary>Applications that reached Applied or beyond (the funnel's denominator).</summary>
    public int AppliedApplications { get; set; }

    public double? InterviewRatePercent { get; set; }
    public double? AverageDaysToFirstReply { get; set; }

    public List<FunnelStageDto> Funnel { get; set; } = new();
    public List<SourceReplyDto> Sources { get; set; } = new();
    public List<StageDurationDto> StageDurations { get; set; } = new();
    public List<RejectionReasonDto> RejectionReasons { get; set; } = new();
}

public class FunnelStageDto
{
    public string Stage { get; set; } = string.Empty;
    public int Count { get; set; }
    public double? Percent { get; set; }
}

public class SourceReplyDto
{
    public string Source { get; set; } = string.Empty;
    public int Applications { get; set; }
    public int Replies { get; set; }
    public bool EnoughData { get; set; }

    /// <summary>Null unless <see cref="EnoughData"/>.</summary>
    public double? ReplyRatePercent { get; set; }
}

public class StageDurationDto
{
    public string Stage { get; set; } = string.Empty;
    public double? AverageDays { get; set; }
    public int Samples { get; set; }
}

public class RejectionReasonDto
{
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }
}
