using System.Reflection;
using SkyRoute.Application.Contracts;
using SkyRoute.Infrastructure.Providers;

namespace SkyRoute.Application.Tests.Providers;

/// <summary>
/// Unit tests for BudgetWingsProvider (test-strategy.md Section 1.1, feature-provider-
/// aggregation.md Sections 3.2/4/4.1). Asserts against the fixed, documented 4-flight
/// schedule and the BR-002 pricing rule (finalPrice = max(round(baseFare * 0.90, 2), 29.99)).
///
/// Route-filtering fix (requirements-compliance follow-up, reversing ASM-006/OQ-003):
/// SearchAsync now only returns schedule entries whose Origin/Destination match the
/// request, so every test below passes the origin/destination that actually matches the
/// flight under test rather than relying on the old "always return the full schedule"
/// behavior. Default origin/destination (LHR/JFK) matches BW210.
/// </summary>
public class BudgetWingsProviderTests
{
    private readonly BudgetWingsProvider _provider = new();

    private static SearchRequest MakeRequest(
        string cabinClass = "Economy",
        DateOnly? departureDate = null,
        string origin = "LHR",
        string destination = "JFK") => new()
    {
        Origin = origin,
        Destination = destination,
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

    /// <summary>
    /// Route-filtering fix: only the schedule entry whose Origin/Destination exactly
    /// matches the requested route is returned, one flight per known BudgetWings route
    /// (feature-provider-aggregation.md Section 3.2's 4 fixed entries, each on a distinct
    /// route). Case-insensitivity is covered separately below.
    /// </summary>
    [Theory]
    [InlineData("LHR", "JFK", "BW210")]
    [InlineData("SYD", "LAX", "BW225")]
    [InlineData("LAX", "JFK", "BW238")]
    [InlineData("MAN", "LHR", "BW241")]
    public async Task SearchAsync_FiltersToRequestedRoute_ReturnsOnlyTheMatchingFixedFlight(
        string origin, string destination, string expectedFlightNumber)
    {
        var results = await _provider.SearchAsync(MakeRequest(origin: origin, destination: destination), CancellationToken.None);

        var flight = Assert.Single(results);
        Assert.Equal("BudgetWings", flight.Provider);
        Assert.Equal(expectedFlightNumber, flight.FlightNumber);
    }

    /// <summary>
    /// Route-filtering fix: matching is case-insensitive on the airport code (consistent
    /// with the codebase's existing convention that Origin/Destination are validated
    /// upstream by SearchRequestValidator's case-sensitive uppercase-only regex before ever
    /// reaching a provider — this is defense in depth for any caller that bypasses that
    /// validator, e.g. a direct unit test).
    /// </summary>
    [Fact]
    public async Task SearchAsync_MatchesRoute_CaseInsensitively()
    {
        var results = await _provider.SearchAsync(MakeRequest(origin: "lhr", destination: "jfk"), CancellationToken.None);

        var flight = Assert.Single(results);
        Assert.Equal("BW210", flight.FlightNumber);
    }

    /// <summary>
    /// Route-filtering fix: a route with no scheduled flight in this provider's fixed
    /// dataset must return an empty list, not an error — preserving the existing "empty
    /// state" UI contract (feature-provider-aggregation.md Section 2).
    /// </summary>
    [Fact]
    public async Task SearchAsync_NoScheduledFlightForRoute_ReturnsEmptyList()
    {
        // BudgetWings' only LHR/MAN-adjacent entries are LHR->JFK and MAN->LHR — the reverse
        // direction, LHR->MAN, has no fixed entry in either provider's schedule.
        var results = await _provider.SearchAsync(MakeRequest(origin: "LHR", destination: "MAN"), CancellationToken.None);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData("BW241", "MAN", "LHR", "Economy", 60.00, 54.00)]
    [InlineData("BW238", "LAX", "JFK", "Economy", 150.00, 135.00)]
    public async Task SearchAsync_AppliesBR002PricingFormula_PerWorkedExamples(
        string flightNumber, string origin, string destination, string cabinClass,
        decimal expectedBaseFare, decimal expectedPricePerPassenger)
    {
        var results = await _provider.SearchAsync(MakeRequest(cabinClass, origin: origin, destination: destination), CancellationToken.None);
        var flight = Assert.Single(results, r => r.FlightNumber == flightNumber);

        Assert.Equal(expectedBaseFare, flight.BaseFare);
        Assert.Equal(expectedPricePerPassenger, flight.PricePerPassenger);
    }

    [Fact]
    public async Task SearchAsync_CabinClassMultipliers_ScaleBaseFareRelativeToEconomy()
    {
        var economy = await _provider.SearchAsync(MakeRequest("Economy", origin: "LAX", destination: "JFK"), CancellationToken.None);
        var business = await _provider.SearchAsync(MakeRequest("Business", origin: "LAX", destination: "JFK"), CancellationToken.None);
        var first = await _provider.SearchAsync(MakeRequest("First Class", origin: "LAX", destination: "JFK"), CancellationToken.None);

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
        var results = await _provider.SearchAsync(
            MakeRequest(departureDate: new DateOnly(2026, 8, 1), origin: "SYD", destination: "LAX"), CancellationToken.None);
        var bw225 = results.Single(r => r.FlightNumber == "BW225");

        Assert.Equal(new DateTime(2026, 8, 1, 23, 0, 0, DateTimeKind.Utc), bw225.DepartureDateTime);
        Assert.Equal(new DateTime(2026, 8, 2, 12, 0, 0, DateTimeKind.Utc), bw225.ArrivalDateTime);
    }

    [Fact]
    public async Task SearchAsync_DepartureDateTime_UsesRequestedDate_WithFixedTimeOfDay()
    {
        var requestedDate = new DateOnly(2026, 9, 15);
        var results = await _provider.SearchAsync(
            MakeRequest(departureDate: requestedDate, origin: "MAN", destination: "LHR"), CancellationToken.None);

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

    /// <summary>
    /// SEC-001 (Phase 16 security review) — TryResolveFare must reproduce exactly the same
    /// fare SearchAsync would return for the same flight number/cabin class combination
    /// (same worked examples as SearchAsync_AppliesBR002PricingFormula_PerWorkedExamples),
    /// since BookingService relies on this method to authoritatively re-derive the fare at
    /// booking time rather than trusting the client-submitted snapshot.
    /// </summary>
    [Theory]
    [InlineData("BW241", "Economy", 60.00, 54.00)]
    [InlineData("BW238", "Economy", 150.00, 135.00)]
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
        var resolved = _provider.TryResolveFare("BW999", "Economy", out var baseFare, out var pricePerPassenger);

        Assert.False(resolved);
        Assert.Equal(0m, baseFare);
        Assert.Equal(0m, pricePerPassenger);
    }
}
