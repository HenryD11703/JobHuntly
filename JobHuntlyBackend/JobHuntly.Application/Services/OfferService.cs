using JobHuntly.Application.DTOs;
using JobHuntly.Application.Exceptions;
using JobHuntly.Application.Interfaces;
using JobHuntly.Application.Logic;
using JobHuntly.Domain.Entities;

namespace JobHuntly.Application.Services;

public class OfferService(
    IOfferRepository offers,
    IJobApplicationRepository applications,
    IUserRepository users,
    TimeProvider clock) : IOfferService
{
    public async Task<OfferComparisonDto> GetComparisonAsync(Guid userId)
    {
        var user = await users.GetByIdAsync(userId) ?? throw new NotFoundException("User not found.");
        var rates = new ExchangeRates(user.UsdToGtq, user.EurToGtq);

        var all = await offers.GetAllByUserIdAsync(userId);
        return new OfferComparisonDto
        {
            Rates = new RatesDto { UsdToGtq = user.UsdToGtq, EurToGtq = user.EurToGtq },
            Offers = all
                .Select(o => Mapper.ToDto(o, rates))
                .OrderByDescending(o => o.MonthlyUsd)
                .ToList()
        };
    }

    public async Task<OfferDto> AddOfferAsync(Guid applicationId, CreateOfferDto dto, Guid userId)
    {
        var application = await applications.GetByIdAsync(applicationId, userId)
                          ?? throw new NotFoundException("Application not found.");
        var user = await users.GetByIdAsync(userId) ?? throw new NotFoundException("User not found.");
        var now = clock.GetUtcNow().UtcDateTime;

        var offer = new Offer
        {
            Id = Guid.NewGuid(),
            JobApplicationId = application.Id,
            JobApplication = application,
            Amount = dto.Amount,
            Currency = dto.Currency,
            Period = dto.Period,
            Notes = dto.Notes,
            CreatedAt = now
        };
        application.Events.Add(new ApplicationEvent
        {
            Id = Guid.NewGuid(),
            Type = EventType.OfferAdded,
            OccurredAt = now,
            Note = $"{dto.Amount} {dto.Currency} {dto.Period}"
        });

        await offers.AddAsync(offer);
        return Mapper.ToDto(offer, new ExchangeRates(user.UsdToGtq, user.EurToGtq));
    }

    public async Task DeleteOfferAsync(Guid offerId, Guid userId)
    {
        var offer = await offers.GetByIdAsync(offerId, userId) ?? throw new NotFoundException("Offer not found.");
        await offers.DeleteAsync(offer);
    }
}
