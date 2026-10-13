using JobHuntly.Infrastructure.Rates;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace JobHuntly.Tests.Integration;

/// <summary>
/// Calls real third-party services. Skipped unless RUN_REAL_API_TESTS=1, so CI and normal runs never
/// depend on the internet. Run it by hand when touching a provider (principle 9).
/// </summary>
public sealed class RealApiFactAttribute : FactAttribute
{
    public RealApiFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("RUN_REAL_API_TESTS") != "1")
            Skip = "Set RUN_REAL_API_TESTS=1 to call the real service.";
    }
}

public class RealServicesTests
{
    [RealApiFact] // AC-7.5 (manual verification)
    public async Task Open_er_api_returns_plausible_gtq_and_eur_rates()
    {
        using var http = new HttpClient();
        var provider = new OpenErApiRateProvider(http, new MemoryCache(new MemoryCacheOptions()), NullLogger<OpenErApiRateProvider>.Instance);

        var quote = await provider.GetCurrentAsync(CancellationToken.None);

        Console.WriteLine($"REAL RATES: 1 USD = {quote.UsdToGtq} GTQ, 1 EUR = {quote.EurToGtq} GTQ, provider updated {quote.ProviderUpdatedAtUtc:u}");
        Assert.InRange(quote.UsdToGtq, 6m, 9m);
        Assert.InRange(quote.EurToGtq, 6m, 12m);
        Assert.True(quote.ProviderUpdatedAtUtc > DateTime.UtcNow.AddDays(-3));

        // Second call is served from the cache.
        var again = await provider.GetCurrentAsync(CancellationToken.None);
        Assert.Same(quote, again);
    }
}
