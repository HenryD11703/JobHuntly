using System.Globalization;
using System.Text.Json;
using JobHuntly.Application.Exceptions;
using JobHuntly.Application.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JobHuntly.Infrastructure.Rates;

/// <summary>
/// Suggested exchange rates from open.er-api.com (ExchangeRate-API open access): no key, updated daily,
/// includes GTQ. Their terms require attribution, which the API returns and the UI shows.
/// The response is cached for 12 hours and validated before anything is returned.
/// </summary>
public class OpenErApiRateProvider(HttpClient http, IMemoryCache cache, ILogger<OpenErApiRateProvider> logger, IConfiguration? configuration = null)
    : IExchangeRateProvider
{
    public const string Endpoint = "https://open.er-api.com/v6/latest/USD";
    public const string Attribution = "https://www.exchangerate-api.com";
    private const string CacheKey = "rates:open-er-api";
    private static readonly TimeSpan CacheFor = TimeSpan.FromHours(12);

    public async Task<RateQuote> GetCurrentAsync(CancellationToken ct)
    {
        if (cache.TryGetValue(CacheKey, out RateQuote? cached) && cached is not null) return cached;

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(10));

            // Rates:Endpoint exists so end-to-end tests can point at a local mock instead of the real service.
            var endpoint = configuration?["Rates:Endpoint"] is { Length: > 0 } custom ? custom : Endpoint;
            using var response = await http.GetAsync(endpoint, timeout.Token);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: timeout.Token);

            var quote = Parse(document.RootElement);
            cache.Set(CacheKey, quote, CacheFor);
            return quote;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or InvalidDataException)
        {
            logger.LogWarning(ex, "Exchange rate provider failed");
            throw new ExternalServiceException("Exchange rates are not available right now. Type the rates manually.");
        }
    }

    /// <summary>Parses and validates the provider payload. Public so tests can exercise it without the network.</summary>
    public static RateQuote Parse(JsonElement root)
    {
        if (!root.TryGetProperty("result", out var result) || result.GetString() != "success")
            throw new InvalidDataException("Provider did not report success.");
        if (!root.TryGetProperty("rates", out var rates))
            throw new InvalidDataException("Provider response has no rates.");

        var gtqPerUsd = ReadRate(rates, "GTQ");
        var eurPerUsd = ReadRate(rates, "EUR");

        // Plausible range check: catches a broken or swapped provider response, not market moves.
        if (gtqPerUsd < 1m || gtqPerUsd > 20m)
            throw new InvalidDataException("GTQ rate is outside the plausible range.");

        var eurToGtq = Math.Round(gtqPerUsd / eurPerUsd, 4, MidpointRounding.AwayFromZero);

        var updated = root.TryGetProperty("time_last_update_unix", out var unix) && unix.TryGetInt64(out var seconds)
            ? DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime
            : DateTime.UtcNow;

        return new RateQuote(Math.Round(gtqPerUsd, 4, MidpointRounding.AwayFromZero), eurToGtq, updated, "ExchangeRate-API", Attribution);
    }

    private static decimal ReadRate(JsonElement rates, string code)
    {
        if (!rates.TryGetProperty(code, out var element) || !element.TryGetDecimal(out var value) || value <= 0)
            throw new InvalidDataException($"Provider response has no valid {code} rate.");
        return value;
    }
}
