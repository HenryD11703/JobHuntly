using JobHuntly.Application.Demo;
using JobHuntly.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JobHuntly.Infrastructure.Demo;

/// <summary>Deletes demo sandboxes (and, through cascading deletes, all their data) after 24 hours.</summary>
public class DemoCleaner(AppDbContext context, TimeProvider clock)
{
    public async Task<int> DeleteExpiredAsync(CancellationToken ct = default)
    {
        var cutoff = clock.GetUtcNow().UtcDateTime - DemoSeed.Lifetime;
        return await context.Users
            .Where(u => u.IsDemo && u.CreatedAt < cutoff)
            .ExecuteDeleteAsync(ct);
    }
}

/// <summary>Runs <see cref="DemoCleaner"/> shortly after startup and then every hour.</summary>
public class DemoCleanupService(IServiceScopeFactory scopes, ILogger<DemoCleanupService> logger) : BackgroundService
{
    private static readonly TimeSpan FirstRunDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(FirstRunDelay, stoppingToken);
            using var timer = new PeriodicTimer(Interval);
            do
            {
                await RunOnceAsync(stoppingToken);
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException)
        {
            // shutting down
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            using var scope = scopes.CreateScope();
            var deleted = await scope.ServiceProvider.GetRequiredService<DemoCleaner>().DeleteExpiredAsync(ct);
            if (deleted > 0) logger.LogInformation("Deleted {Count} expired demo sandboxes", deleted);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Demo cleanup failed; it will try again next hour");
        }
    }
}
