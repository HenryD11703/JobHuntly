using JobHuntly.Domain.Entities;

namespace JobHuntly.Application.Logic;

public sealed record ExchangeRates(decimal UsdToGtq, decimal EurToGtq);

public sealed record NormalizedOffer(decimal MonthlyUsd, decimal MonthlyGtq);

/// <summary>
/// Converts any offer to a monthly figure in USD and GTQ using the user's saved rates.
/// Gross amounts as entered: no tax logic. Rounding happens once, at the end, to 2 decimals,
/// half away from zero.
/// </summary>
public static class OfferComparator
{
    public static NormalizedOffer Normalize(decimal amount, Currency currency, PayPeriod period, ExchangeRates rates)
    {
        if (rates.UsdToGtq <= 0 || rates.EurToGtq <= 0)
            throw new ArgumentOutOfRangeException(nameof(rates), "Exchange rates must be positive.");

        var monthly = period == PayPeriod.Annual ? amount / 12m : amount;

        var monthlyGtq = currency switch
        {
            Currency.GTQ => monthly,
            Currency.USD => monthly * rates.UsdToGtq,
            Currency.EUR => monthly * rates.EurToGtq,
            _ => throw new ArgumentOutOfRangeException(nameof(currency))
        };

        var monthlyUsd = currency == Currency.USD ? monthly : monthlyGtq / rates.UsdToGtq;

        return new NormalizedOffer(Round(monthlyUsd), Round(monthlyGtq));
    }

    public static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
