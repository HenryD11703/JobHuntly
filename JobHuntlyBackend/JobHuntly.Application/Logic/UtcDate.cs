namespace JobHuntly.Application.Logic;

public static class UtcDate
{
    /// <summary>
    /// PostgreSQL timestamptz only accepts UTC values. Dates without an offset are treated as UTC,
    /// dates with an offset are converted.
    /// </summary>
    public static DateTime Normalize(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
