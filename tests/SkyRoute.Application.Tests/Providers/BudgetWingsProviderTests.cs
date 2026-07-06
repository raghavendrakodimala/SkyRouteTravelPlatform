using System.Reflection;
using SkyRoute.Application.Contracts;
using SkyRoute.Infrastructure.Providers;

namespace SkyRoute.Application.Tests.Providers;

/// <summary>
/// Unit tests for BudgetWingsProvider (test-strategy.md Section 1.1, feature-provider-
/// aggregation.md Sections 3.2/4/4.1). Asserts against the fixed, documented 4-flight
/// schedule and the BR-002 pricing rule (finalPrice = max(round(baseFare * 0.90, 2), 29.99)).
/// </summary>
public class BudgetWingsProviderTests
{
    private readonly BudgetWingsProvider _provider = new();

    private static SearchRequest MakeRequest(string cabinClass = "Economy", DateOnly? departureDate = null) => new()
    {
        Origin = "LHR",
        Destination = "JFK",
        DepartureDate = departureDate ?? new DateOnly(2026, 8, 1),
        PassengerCount = 2,
        CabinClass = cabinClass,
        TripType = "OneWay",
    };

    [Fact]
    public void ProviderName_IsExactlyBudgetWings()
    {
        Assert.Equal("BudgetWings", _provider.ProviderName);
    }

    [Fact]
    public async Task SearchAsync_ReturnsExactlyFourFixedFlights()
    {
        var results = await _provider.SearchAsync(MakeRequest(), CancellationToken.None);

        Assert.Equal(4, results.Count);
        Assert.All(results, r => Assert.Equal("BudgetWings", r.Provider));
        Assert.Contains(results, r => r.FlightNumber == "BW210");
        Assert.Contains(results, r => r.FlightNumber == "BW225");
        Assert.Contains(results, r => r.FlightNumber == "BW238");
        Assert.Contains(results, r => r.FlightNumber == "BW241");
    }

    [Theory]
    [InlineData("BW241", "Economy", 60.00, 54.00)]
    [InlineData("BW238", "Economy", 150.00, 135.00)]
    public async Task SearchAsync_AppliesBR002PricingFormula_PerWorkedExamples(
        string flightNumber, string cabinClass, decimal expectedBaseFare, decimal expectedPricePerPassenger)
    {
        var results = await _provider.SearchAsync(MakeRequest(cabinClass), CancellationToken.None);
        var flight = Assert.Single(results, r => r.FlightNumber == flightNumber);

        Assert.Equal(expectedBaseFare, flight.BaseFare);
        Assert.Equal(expectedPricePerPassenger, flight.PricePerPassenger);
    }

    [Fact]
    public async Task SearchAsync_CabinClassMultipliers_ScaleBaseFareRelativeToEconomy()
    {
        var economy = await _provider.SearchAsync(MakeRequest("Economy"), CancellationToken.None);
        var business = await _provider.SearchAsync(MakeRequest("Business"), CancellationToken.None);
        var first = await _provider.SearchAsync(MakeRequest("First Class"), CancellationToken.None);

        var economyBase = economy.Single(r => r.FlightNumber == "BW238").BaseFare;
        var businessBase = business.Single(r => r.FlightNumber == "BW238").BaseFare;
        var firstBase = first.Single(r => r.FlightNumber == "BW238").BaseFare;

        Assert.Equal(economyBase * 2.0m, businessBase);
        Assert.Equal(economyBase * 3.5m, firstBase);
    }

    [Fact]
    public async Task SearchAsync_ArrivalDateTime_RollsOverToNextCalendarDay_WhenDurationCrossesMidnight()
    {
        // BW225 departs 23:00, duration 780 minutes (13h) -> arrives 12:00 the next day
        // (feature-provider-aggregation.md Section 5).
        var results = await _provider.SearchAsync(MakeRequest(departureDate: new DateOnly(2026, 8, 1)), CancellationToken.None);
        var bw225 = results.Single(r => r.FlightNumber == "BW225");

        Assert.Equal(new DateTime(2026, 8, 1, 23, 0, 0, DateTimeKind.Utc), bw225.DepartureDateTime);
        Assert.Equal(new DateTime(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc), bw225.ArrivalDateTime);
    }

    [Fact]
    public async Task SearchAsync_DepartureDateTime_UsesRequestedDate_WithFixedTimeOfDay()
    {
        var requestedDate = new DateOnly(2026, 9, 15);
        var results = await _provider.SearchAsync(MakeRequest(departureDate: requestedDate), CancellationToken.None);

        var bw241 = results.Single(r => r.FlightNumber == "BW241");

        Assert.Equal(new DateTime(2026, 9, 15, 14, 0, 0, DateTimeKind.Utc), bw241.DepartureDateTime);
    }

    /// <summary>
    /// Reflection-based isolation tests for the private, named ApplyBudgetWingsPricing(decimal)
    /// method — hand-picked boundary values per test-strategy.md Section 3, including the
    /// explicit floor boundary cases from docs/specs/non-functional-requirements.md ($25.00 ->
    /// $29.99 floor; $30.00 -> $27.00 -> $29.99 floor, proving round-then-floor order per
    /// architecture-plan.md Section 3.1).
    /// </summary>
    [Theory]
    [InlineData(25.00, 29.99)]
    [InlineData(30.00, 29.99)]
    [InlineData(100.00, 90.00)]
    [InlineData(200.00, 180.00)]
    public void ApplyBudgetWingsPricing_AppliesDiscountThenFloor_PerBR002(decimal baseFare, decimal expected)
    {
        var method = typeof(BudgetWingsProvider).GetMethod(
            "ApplyBudgetWingsPricing", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        var actual = (decimal)method!.Invoke(null, new object[] { baseFare })!;

        Assert.Equal(expected, actual);
    }
}
