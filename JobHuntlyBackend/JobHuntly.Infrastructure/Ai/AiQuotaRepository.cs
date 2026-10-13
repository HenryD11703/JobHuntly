using JobHuntly.Application.Ai;
using JobHuntly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace JobHuntly.Infrastructure.Ai;

/// <summary>
/// Daily AI allowance stored in PostgreSQL. The increment is one atomic statement, so two parallel
/// requests can never both slip past the limit.
/// </summary>
public class AiQuotaRepository(AppDbContext context) : IAiQuota
{
    public async Task<bool> TryConsumeAsync(Guid userId, DateOnly day, int limit)
    {
        if (limit <= 0) return false;

        var rows = await context.Database.ExecuteSqlInterpolatedAsync($@"
            INSERT INTO ""AiUsage"" (""UserId"", ""Day"", ""Count"")
            VALUES ({userId}, {day}, 1)
            ON CONFLICT (""UserId"", ""Day"")
            DO UPDATE SET ""Count"" = ""AiUsage"".""Count"" + 1
            WHERE ""AiUsage"".""Count"" < {limit}");

        return rows == 1;
    }

    public async Task ReleaseAsync(Guid userId, DateOnly day)
    {
        await context.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE ""AiUsage"" SET ""Count"" = GREATEST(""Count"" - 1, 0)
            WHERE ""UserId"" = {userId} AND ""Day"" = {day}");
    }

    public async Task<int> GetUsedAsync(Guid userId, DateOnly day) =>
        await context.AiUsages.Where(a => a.UserId == userId && a.Day == day)
            .Select(a => a.Count).FirstOrDefaultAsync();
}
