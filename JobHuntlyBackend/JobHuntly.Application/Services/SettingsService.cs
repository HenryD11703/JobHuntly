using JobHuntly.Application.DTOs;
using JobHuntly.Application.Exceptions;
using JobHuntly.Application.Interfaces;

namespace JobHuntly.Application.Services;

public class SettingsService(IUserRepository users, IExchangeRateProvider rateProvider) : ISettingsService
{
    public async Task<RatesDto> GetRatesAsync(Guid userId)
    {
        var user = await users.GetByIdAsync(userId) ?? throw new NotFoundException("User not found.");
        return new RatesDto { UsdToGtq = user.UsdToGtq, EurToGtq = user.EurToGtq };
    }

    public async Task<RatesDto> UpdateRatesAsync(RatesDto dto, Guid userId)
    {
        var user = await users.GetByIdAsync(userId) ?? throw new NotFoundException("User not found.");
        user.UsdToGtq = dto.UsdToGtq;
        user.EurToGtq = dto.EurToGtq;
        await users.SaveAsync();
        return new RatesDto { UsdToGtq = user.UsdToGtq, EurToGtq = user.EurToGtq };
    }

    /// <summary>Only suggests values; nothing is saved until the user confirms with <see cref="UpdateRatesAsync"/>.</summary>
    public async Task<SuggestedRatesDto> GetSuggestedRatesAsync(CancellationToken ct)
    {
        var quote = await rateProvider.GetCurrentAsync(ct);
        return new SuggestedRatesDto
        {
            UsdToGtq = quote.UsdToGtq,
            EurToGtq = quote.EurToGtq,
            ProviderUpdatedAt = quote.ProviderUpdatedAtUtc,
            Provider = quote.Provider,
            AttributionUrl = quote.AttributionUrl
        };
    }
}
