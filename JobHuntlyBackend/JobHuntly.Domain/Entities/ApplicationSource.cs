namespace JobHuntly.Domain.Entities;

/// <summary>Where the opportunity came from. Used for reply-rate insights.</summary>
public enum ApplicationSource
{
    Other = 0,
    LinkedIn = 1,
    Referral = 2,
    CompanySite = 3,
    Recruiter = 4
}
