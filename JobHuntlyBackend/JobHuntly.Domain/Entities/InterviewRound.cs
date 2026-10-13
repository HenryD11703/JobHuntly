namespace JobHuntly.Domain.Entities;

public enum InterviewRound
{
    HrScreen = 0,
    Technical = 1,
    Behavioral = 2,
    TakeHome = 3,
    Final = 4,
    Other = 5
}

public enum InterviewStatus
{
    Scheduled = 0,
    Done = 1,
    Cancelled = 2
}
