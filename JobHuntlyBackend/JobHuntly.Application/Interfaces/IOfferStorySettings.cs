using JobHuntly.Application.DTOs;
using JobHuntly.Application.Logic;
using JobHuntly.Domain.Entities;

namespace JobHuntly.Application.Interfaces;

public interface IOfferRepository
{
    /// <summary>Offers of the user's applications, with the application loaded (company and position).</summary>
    Task<List<Offer>> GetAllByUserIdAsync(Guid userId);
    Task<Offer?> GetByIdAsync(Guid id, Guid userId);
    Task AddAsync(Offer offer);
    Task DeleteAsync(Offer offer);
}

public interface IStoryRepository
{
    Task<List<Story>> GetAllByUserIdAsync(Guid userId);
    Task<Story?> GetByIdAsync(Guid id, Guid userId);
    Task AddAsync(Story story);
    Task SaveAsync();
    Task DeleteAsync(Story story);
}

public interface IOfferService
{
    Task<OfferComparisonDto> GetComparisonAsync(Guid userId);
    Task<OfferDto> AddOfferAsync(Guid applicationId, CreateOfferDto dto, Guid userId);
    Task DeleteOfferAsync(Guid offerId, Guid userId);
}

public interface IStoryService
{
    Task<IEnumerable<StoryDto>> GetAllAsync(Guid userId);
    Task<StoryDto> CreateAsync(SaveStoryDto dto, Guid userId);
    Task<StoryDto> UpdateAsync(Guid id, SaveStoryDto dto, Guid userId);
    Task DeleteAsync(Guid id, Guid userId);
}

public interface INudgeService
{
    Task<IReadOnlyList<Nudge>> GetTodayAsync(Guid userId);
}

public interface ISettingsService
{
    Task<RatesDto> GetRatesAsync(Guid userId);
    Task<RatesDto> UpdateRatesAsync(RatesDto dto, Guid userId);
    Task<SuggestedRatesDto> GetSuggestedRatesAsync(CancellationToken ct);
}

public sealed record RateQuote(
    decimal UsdToGtq,
    decimal EurToGtq,
    DateTime ProviderUpdatedAtUtc,
    string Provider,
    string AttributionUrl);

/// <summary>Source of suggested exchange rates. It only suggests: saved rates drive every calculation.</summary>
public interface IExchangeRateProvider
{
    Task<RateQuote> GetCurrentAsync(CancellationToken ct);
}
