using System.Reflection;
using SkyRoute.Application.Contracts;
using SkyRoute.Infrastructure.Providers;

namespace SkyRoute.Application.Tests.Providers;

/// <summary>
/// Unit tests for GlobalAirProvider (test-strategy.md Section 1.1, feature-provider-
/// aggregation.md Sections 3.1/4/4.1). Asserts against the fixed, documented 4-flight
/// schedule and the BR-001 pricing rule (finalPrice = round(baseFare * 1.15, 2)).
/// </summary>
public class GlobalAirProviderTests
{
    private readonly GlobalAirProvider _provider = new();

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
    public void ProviderName_IsExactlyGlobalAir()
    {
        Assert.Equal("GlobalAir", _provider.ProviderName);
    }

    [Fact]
    public async Task SearchAsync_ReturnsExactlyFourFixedFlights()
    {
        var results = await _provider.SearchAsync(MakeRequest(), CancellationToken.None);

        Assert.Equal(4, results.Count);
        Assert.All(results, r => Assert.Equal("GlobalAir", r.Provider));
        Assert.Contains(results, r => r.FlightNumber == "GA101");
        Assert.Contains(results, r => r.FlightNumber == "GA204");
        Assert.Contains(results, r => r.FlightNumber == "GA309");
        Assert.Contains(results, r => r.FlightNumber == "GA412");
    }

    [Theory]
    [InlineData("GA101", "Economy", 250.00, 287.50)]
    [InlineData("GA101", "Business", 500.00, 575.00)]
    [InlineData("GA101", "First Class", 875.00, 1006.25)]
    [InlineData("GA412", "Economy", 80.00, 92.00)]
    public async Task SearchAsync_AppliesBR001PricingFormula_PerWorkedExamples(
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

        var economyBase = economy.Single(r => r.FlightNumber == "GA309").BaseFare;
        var businessBase = business.Single(r => r.FlightNumber == "GA309").BaseFare;
        var firstBase = first.Single(r => r.FlightNumber == "GA309").BaseFare;

        Assert.Equal(economyBase * 2.0m, businessBase);
        Assert.Equal(economyBase * 3.5m, firstBase);
    }

    [Fact]
    public async Task SearchAsync_DepartureDateTime_UsesRequestedDate_WithFixedTimeOfDay()
    {
        var requestedDate = new DateOnly(2026, 9, 15);
        var results = await _provider.SearchAsync(MakeRequest(departureDate: requestedDate), CancellationToken.None);

        var ga101 = results.Single(r => r.FlightNumber == "GA101");

        Assert.Equal(new DateTime(2026, 9, 15, 9, 0, 0, DateTimeKind.Utc), ga101.DepartureDateTime);
    }

    [Fact]
    public async Task SearchAsync_ArrivalDateTime_RollsOverToNextCalendarDay_WhenDurationCrossesMidnight()
    {
        // GA204 departs 22:00, duration 450 minutes (7h30m) -> arrives 05:30 the next day
        // (feature-provider-aggregation.md Section 5).
        var results = await _provider.SearchAsync(MakeRequest(departureDate: new DateOnly(2026, 8, 1)), CancellationToken.None);
        var ga204 = results.Single(r => r.FlightNumber == "GA204");

        Assert.Equal(new DateTime(2026, 8, 1, 22, 0, 0, DateTimeKind.Utc), ga204.DepartureDateTime);
        Assert.Equal(new DateTime(2026, 8, 2, 5, 30, 0, DateTimeKind.Utc), ga204.ArrivalDateTime);
    }

    [Fact]
    public async Task SearchAsync_EchoesRequestedCabinClass_OnEveryResult()
    {
        var results = await _provider.SearchAsync(MakeRequest("Business"), CancellationToken.None);

        Assert.All(results, r => Assert.Equal("Business", r.CabinClass));
    }

    /// <summary>
    /// Reflection-based isolation tests for the private, named ApplyGlobalAirPricing(decimal)
    /// method (DP-006/DP-019/AD-008 intent: pricing must be independently unit-testable given
    /// only a decimal base fare, with hand-picked boundary values per test-strategy.md Section
    /// 3 — not limited to the fixed dataset's own base fares). The method is intentionally
    /// private per the provider's own encapsulation; reflection is used here as a test-only
    /// technique so production code visibility does not need to change to satisfy this
    /// strategy requirement.
    /// </summary>
    [Theory]
    [InlineData(87.50, 100.63)] // BR-001 generic rounding example (requirements.md / feature-provider-aggregation.md Section 4.1)
    [InlineData(100.00, 115.00)]
    [InlineData(0.00, 0.00)]
    public void ApplyGlobalAirPricing_RoundsToTwoDecimalPlaces_PerBR001(decimal baseFare, decimal expected)
    {
        var method = typeof(GlobalAirProvider).GetMethod(
            "ApplyGlobalAirPricing", BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        var actual = (decimal)method!.Invoke(null, new object[] { baseFare })!;

        Assert.Equal(expected, actual);
    }

    /// <summary>
    /// SEC-001 (Phase 16 security review) — TryResolveFare must reproduce exactly the same
    /// fare SearchAsync would return for the same flight number/cabin class combination
    /// (same worked examples as SearchAsync_AppliesBR001PricingFormula_PerWorkedExamples),
    /// since BookingService relies on this method to authoritatively re-derive the fare at
    /// booking time rather than trusting the client-submitted snapshot.
    /// </summary>
    [Theory]
    [InlineData("GA101", "Economy", 250.00, 287.50)]
    [InlineData("GA101", "Business", 500.00, 575.00)]
    [InlineData("GA101", "First Class", 875.00, 1006.25)]
    [InlineData("GA412", "Economy", 80.00, 92.00)]
    public void TryResolveFare_KnownFlightAndCabinClass_ReturnsTrueWithMatchingFare(
        string flightNumber, string cabinClass, decimal expectedBaseFare, decimal expectedPricePerPassenger)
    {
        var resolved = _provider.TryResolveFare(flightNumber, cabinClass, out var baseFare, out var pricePerPassenger);

        Assert.True(resolved);
        Assert.Equal(expectedBaseFare, baseFare);
        Assert.Equal(expectedPricePerPassenger, pricePerPassenger);
    }

    [Fact]
    public void TryResolveFare_UnknownFlightNumber_ReturnsFalseWithZeroedOutValues()
    {
        var resolved = _provider.TryResolveFare("GA999", "Economy", out var baseFare, out var pricePerPassenger);

        Assert.False(resolved);
        Assert.Equal(0m, baseFare);
        Assert.Equal(0m, pricePerPassenger);
    }

    [Fact]
    public void TryResolveFare_FlightNumberIsCaseSensitive_UnlikeAnywhereElseInThisSchedule()
    {
        // Ordinal (case-sensitive) match by design — FlightNumber is a server-defined,
        // fixed-format identifier (not user free text), so there is no legitimate reason
        // for a lowercase variant to resolve.
        var resolved = _provider.TryResolveFare("ga101", "Economy", out _, out _);

        Assert.False(resolved);
    }
}
