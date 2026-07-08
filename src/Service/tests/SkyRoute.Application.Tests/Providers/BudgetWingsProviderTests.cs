using System.Reflection;
using SkyRoute.Application.Dtos;
using SkyRoute.Infrastructure.Providers;

namespace SkyRoute.Application.Tests.Providers;

/// <summary>
/// Unit tests for BudgetWingsProvider (test-strategy.md Section 1.1, feature-provider-
/// aggregation.md Sections 3.2/4/4.1). Asserts against the fixed, documented 4-flight
/// fixture schedule and the BR-002 pricing rule (finalPrice = max(round(baseFare * 0.90, 2), 29.99)).
///
/// Route-filtering fix (requirements-compliance follow-up, reversing ASM-006/OQ-003):
/// SearchAsync now only returns schedule entries whose Origin/Destination match the
/// request, so every test below passes the origin/destination that actually matches the
/// flight under test rather than relying on the old "always return the full schedule"
/// behavior. Default origin/destination (LHR/JFK) matches BW210.
///
/// Route-coverage fix (PO defect, 2026-07-07: "empty state most of the time"): the schedule
/// is now the 4 fixtures PLUS deterministic RouteScheduleGenerator flights (BW departures
/// 10:15 and 21:30, baseFare = round(45 + duration × 0.42, 2)) for every ordered airport
/// pair the fixtures don't cover — so every valid route returns results, while fixture
/// routes still return exactly their documented flights and prices. Sole exception: the
/// deliberate MAN&lt;-&gt;SYD no-direct-service pair (DEC-021, PO 2026-07-08), which keeps the
/// styled empty state demonstrable through a real search.
/// </summary>
public class BudgetWingsProviderTests
{
    private static readonly string[] AirportCodes = { "LHR", "MAN", "JFK", "LAX", "DXB", "SYD" };

    /// <summary>The 4 ordered routes covered by BudgetWings' fixtures (Section 3.2) — each
    /// returns exactly its 1 documented flight; every other ordered pair returns exactly the
    /// 2 generated flights, except the DEC-021 no-direct-service pair below (0 flights).</summary>
    private static readonly HashSet<(string Origin, string Destination)> FixtureRoutes = new()
    {
        ("LHR", "JFK"), ("SYD", "LAX"), ("LAX", "JFK"), ("MAN", "LHR"),
    };

    /// <summary>DEC-021 (PO 2026-07-08): MAN&lt;-&gt;SYD deliberately has no direct service in
    /// either direction from either provider — the demo route for the real empty state.</summary>
    private static readonly HashSet<(string Origin, string Destination)> NoDirectServiceRoutes = new()
    {
        ("MAN", "SYD"), ("SYD", "MAN"),
    };

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
    /// Route-coverage fix: a valid route with no BudgetWings fixture now returns the
    /// deterministic generated flights instead of an empty list. LHR->DXB has a GlobalAir
    /// fixture (GA204) but no BudgetWings one (pair index 3 in RouteScheduleGenerator's
    /// table, duration 420): numbers BW512/BW513 (500 + 3×4 + forward-direction 0, +1 for
    /// the second flight), departures at BudgetWings' generated times 10:15/21:30, baseFare
    /// round(45 + 420 × 0.42, 2) = 221.40, BR-002 price max(221.40 × 0.90, 29.99) = 199.26.
    /// The 21:30 departure also pins the generated overnight-arrival rollover (04:30 next day).
    /// </summary>
    [Fact]
    public async Task SearchAsync_GeneratedRoute_ReturnsExactDeterministicFlights()
    {
        var results = await _provider.SearchAsync(
            MakeRequest(departureDate: new DateOnly(2026, 8, 1), origin: "LHR", destination: "DXB"),
            CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.All(results, r =>
        {
            Assert.Equal("BudgetWings", r.Provider);
            Assert.Equal("LHR", r.Origin);
            Assert.Equal("DXB", r.Destination);
            Assert.Equal(420, r.DurationMinutes);
            Assert.Equal("Economy", r.CabinClass);
            Assert.Equal(221.40m, r.BaseFare);
            Assert.Equal(199.26m, r.PricePerPassenger);
        });

        Assert.Equal("BW512", results[0].FlightNumber);
        Assert.Equal(new DateTime(2026, 8, 1, 10, 15, 0, DateTimeKind.Utc), results[0].DepartureDateTime);
        Assert.Equal(new DateTime(2026, 8, 1, 17, 15, 0, DateTimeKind.Utc), results[0].ArrivalDateTime);

        Assert.Equal("BW513", results[1].FlightNumber);
        Assert.Equal(new DateTime(2026, 8, 1, 21, 30, 0, DateTimeKind.Utc), results[1].DepartureDateTime);
        Assert.Equal(new DateTime(2026, 8, 2, 4, 30, 0, DateTimeKind.Utc), results[1].ArrivalDateTime);
    }

    /// <summary>
    /// Route-coverage fix: generation is pure (no randomness, no clock reads) — two
    /// identical searches must return field-for-field identical results.
    /// </summary>
    [Fact]
    public async Task SearchAsync_GeneratedRoute_IsDeterministicAcrossRepeatedCalls()
    {
        var first = await _provider.SearchAsync(MakeRequest(origin: "JFK", destination: "MAN"), CancellationToken.None);
        var second = await _provider.SearchAsync(MakeRequest(origin: "JFK", destination: "MAN"), CancellationToken.None);

        Assert.Equal(first.Count, second.Count);
        for (var i = 0; i < first.Count; i++)
        {
            Assert.Equal(first[i].FlightNumber, second[i].FlightNumber);
            Assert.Equal(first[i].DepartureDateTime, second[i].DepartureDateTime);
            Assert.Equal(first[i].ArrivalDateTime, second[i].ArrivalDateTime);
            Assert.Equal(first[i].DurationMinutes, second[i].DurationMinutes);
            Assert.Equal(first[i].BaseFare, second[i].BaseFare);
            Assert.Equal(first[i].PricePerPassenger, second[i].PricePerPassenger);
        }
    }

    /// <summary>
    /// Route-coverage fix, fixture-preservation guarantee: a route already covered by the
    /// fixed schedule gets NO generated additions — SYD->LAX still returns exactly BW225
    /// with its documented duration (780, not the generated table's 900 for the LAX-SYD
    /// pair) and unchanged pricing (base 450.00 -> 405.00 per BR-002). LHR->JFK's identical
    /// guarantee (exactly BW210 at 220.00 -> 198.00) is pinned by the route-filter tests
    /// above.
    /// </summary>
    [Fact]
    public async Task SearchAsync_FixtureRoute_IsNotAugmentedByGeneratedFlights()
    {
        var results = await _provider.SearchAsync(MakeRequest(origin: "SYD", destination: "LAX"), CancellationToken.None);

        var flight = Assert.Single(results);
        Assert.Equal("BW225", flight.FlightNumber);
        Assert.Equal(780, flight.DurationMinutes);
        Assert.Equal(450.00m, flight.BaseFare);
        Assert.Equal(405.00m, flight.PricePerPassenger);
    }

    /// <summary>
    /// Route-coverage fix, the PO-defect acceptance test: of the 30 ordered pairs of the 6
    /// supported airports, all 28 served pairs return flights from this provider — exactly
    /// the 1 documented fixture flight on fixture routes, exactly the 2 generated flights
    /// everywhere else — always on the requested route and with unique flight numbers. The
    /// 2 MAN&lt;-&gt;SYD pairs are the deliberate DEC-021 no-direct-service exception (0 flights),
    /// asserted individually below.
    /// </summary>
    [Fact]
    public async Task SearchAsync_EveryOrderedAirportPair_ReturnsExpectedRouteCoverage()
    {
        foreach (var origin in AirportCodes)
        {
            foreach (var destination in AirportCodes.Where(code => code != origin))
            {
                var results = await _provider.SearchAsync(
                    MakeRequest(origin: origin, destination: destination), CancellationToken.None);

                var expectedCount = NoDirectServiceRoutes.Contains((origin, destination))
                    ? 0
                    : FixtureRoutes.Contains((origin, destination)) ? 1 : 2;
                Assert.True(
                    results.Count == expectedCount,
                    $"{origin}->{destination}: expected {expectedCount} flight(s), got {results.Count}");
                Assert.All(results, r =>
                {
                    Assert.Equal(origin, r.Origin);
                    Assert.Equal(destination, r.Destination);
                });
                Assert.Equal(results.Count, results.Select(r => r.FlightNumber).Distinct().Count());
            }
        }
    }

    /// <summary>
    /// DEC-021 (PO 2026-07-08): MAN&lt;-&gt;SYD is the deliberate no-direct-service route — no
    /// fixture and no generated entry in either direction — so a real search can still reach
    /// the styled empty state (challenge PDF 3.2: "a clear empty state if no flights match").
    /// </summary>
    [Theory]
    [InlineData("MAN", "SYD")]
    [InlineData("SYD", "MAN")]
    public async Task SearchAsync_ManSydNoDirectServiceRoute_ReturnsEmptyList(string origin, string destination)
    {
        var results = await _provider.SearchAsync(MakeRequest(origin: origin, destination: destination), CancellationToken.None);

        Assert.Empty(results);
    }

    /// <summary>
    /// An unknown airport code still yields an empty list from the provider (defense in
    /// depth — SearchRequestValidator rejects unknown codes with a 400 long before any
    /// provider runs, so this is unreachable through the API).
    /// </summary>
    [Fact]
    public async Task SearchAsync_UnknownAirportCode_ReturnsEmptyList()
    {
        var results = await _provider.SearchAsync(MakeRequest(origin: "ZZZ", destination: "JFK"), CancellationToken.None);

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
        var resolved = _provider.TryResolveFare(flightNumber, cabinClass, out var baseFare, out var pricePerPassenger, out _, out _);

        Assert.True(resolved);
        Assert.Equal(expectedBaseFare, baseFare);
        Assert.Equal(expectedPricePerPassenger, pricePerPassenger);
    }

    /// <summary>
    /// AUD-025/028/033: TryResolveFare also surfaces the flight's authoritative Origin/
    /// Destination (from the same single schedule lookup) so BookingService can reject a forged
    /// route and derive the BR-003 document rule from the real route.
    /// </summary>
    [Theory]
    [InlineData("BW210", "LHR", "JFK")]
    [InlineData("BW225", "SYD", "LAX")]
    [InlineData("BW241", "MAN", "LHR")]
    public void TryResolveFare_KnownFlight_SurfacesAuthoritativeOriginAndDestination(
        string flightNumber, string expectedOrigin, string expectedDestination)
    {
        var resolved = _provider.TryResolveFare(flightNumber, "Economy", out _, out _, out var origin, out var destination);

        Assert.True(resolved);
        Assert.Equal(expectedOrigin, origin);
        Assert.Equal(expectedDestination, destination);
    }

    /// <summary>
    /// SEC-001 × route-coverage fix: a booking made on a GENERATED flight must re-derive its
    /// fare exactly like a fixture one (same values SearchAsync returns for BW512/BW513 on
    /// LHR->DXB: base 221.40, ×2.0 Business multiplier applied before BR-002).
    /// </summary>
    [Theory]
    [InlineData("BW512", "Economy", 221.40, 199.26)]
    [InlineData("BW513", "Business", 442.80, 398.52)]
    public void TryResolveFare_GeneratedFlight_ReturnsTrueWithMatchingFare(
        string flightNumber, string cabinClass, decimal expectedBaseFare, decimal expectedPricePerPassenger)
    {
        var resolved = _provider.TryResolveFare(flightNumber, cabinClass, out var baseFare, out var pricePerPassenger, out _, out _);

        Assert.True(resolved);
        Assert.Equal(expectedBaseFare, baseFare);
        Assert.Equal(expectedPricePerPassenger, pricePerPassenger);
    }

    [Fact]
    public void TryResolveFare_UnknownFlightNumber_ReturnsFalseWithZeroedOutValues()
    {
        var resolved = _provider.TryResolveFare("BW999", "Economy", out var baseFare, out var pricePerPassenger, out var origin, out var destination);

        Assert.False(resolved);
        Assert.Equal(0m, baseFare);
        Assert.Equal(0m, pricePerPassenger);
        Assert.Null(origin);
        Assert.Null(destination);
    }
}
