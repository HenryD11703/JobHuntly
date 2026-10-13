using JobHuntly.Application.Logic;
using JobHuntly.Domain.Entities;

namespace JobHuntly.Tests.Unit;

/// <summary>AC-7.2 and AC-7.3.</summary>
public class OfferComparatorTests
{
    private static readonly ExchangeRates Rates = new(UsdToGtq: 7.64m, EurToGtq: 8.54m);

    [Fact]
    public void Usd_monthly_is_unchanged_in_usd_and_converted_to_gtq()
    {
        var n = OfferComparator.Normalize(3400m, Currency.USD, PayPeriod.Monthly, Rates);

        Assert.Equal(3400.00m, n.MonthlyUsd);
        Assert.Equal(25976.00m, n.MonthlyGtq);
    }

    [Fact]
    public void Annual_amounts_are_divided_by_twelve_and_euros_go_through_gtq()
    {
        var n = OfferComparator.Normalize(38400m, Currency.EUR, PayPeriod.Annual, Rates);

        Assert.Equal(27328.00m, n.MonthlyGtq);   // 3200 EUR x 8.54
        Assert.Equal(3576.96m, n.MonthlyUsd);    // 27328 / 7.64
    }

    [Fact]
    public void Gtq_monthly_is_unchanged_in_gtq_and_converted_to_usd()
    {
        var n = OfferComparator.Normalize(24000m, Currency.GTQ, PayPeriod.Monthly, Rates);

        Assert.Equal(24000.00m, n.MonthlyGtq);
        Assert.Equal(3141.36m, n.MonthlyUsd);    // 24000 / 7.64
    }

    [Fact]
    public void Rounding_is_half_away_from_zero_to_two_decimals()
    {
        Assert.Equal(2.35m, OfferComparator.Round(2.345m));
        Assert.Equal(2.34m, OfferComparator.Round(2.344m));
        Assert.Equal(-2.35m, OfferComparator.Round(-2.345m));
    }

    [Fact]
    public void Rounding_happens_once_at_the_end()
    {
        // 100 USD / year = 8.3333... per month; x 7.64 = 63.6666... -> 63.67 (rounding the monthly value
        // first, to 8.33, would give 63.64).
        var n = OfferComparator.Normalize(100m, Currency.USD, PayPeriod.Annual, Rates);

        Assert.Equal(63.67m, n.MonthlyGtq);
        Assert.Equal(8.33m, n.MonthlyUsd);
    }

    [Theory]
    [InlineData(0, 8.5)]
    [InlineData(7.6, 0)]
    [InlineData(-1, 8.5)]
    public void Non_positive_rates_are_rejected(double usd, double eur)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            OfferComparator.Normalize(1000m, Currency.USD, PayPeriod.Monthly, new ExchangeRates((decimal)usd, (decimal)eur)));
    }

    [Fact]
    public void Changing_a_rate_changes_the_result_instantly_and_deterministically()
    {
        var before = OfferComparator.Normalize(1000m, Currency.EUR, PayPeriod.Monthly, Rates);
        var after = OfferComparator.Normalize(1000m, Currency.EUR, PayPeriod.Monthly, Rates with { EurToGtq = 9m });

        Assert.Equal(8540.00m, before.MonthlyGtq);
        Assert.Equal(9000.00m, after.MonthlyGtq);
        Assert.Equal(before, OfferComparator.Normalize(1000m, Currency.EUR, PayPeriod.Monthly, Rates));
    }
}
