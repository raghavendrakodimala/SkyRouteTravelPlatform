using System.Reflection;
using SkyRoute.Application.Dtos;
using SkyRoute.Infrastructure.Providers;

namespace SkyRoute.Application.Tests.Providers;

/// <summary>
/// Unit tests for GlobalAirProvider (test-strategy.md Section 1.1, feature-provider-
/// aggregation.md Sections 3.1/4/4.1). Asserts against the fixed, documented 4-flight
/// fixture schedule and the BR-001 pricing rule (finalPrice = round(baseFare * 1.15, 2)).
///
/// Route-filtering fix (requirements-compliance follow-up, reversing ASM-006/OQ-003):
/// SearchAsync now only returns schedule entries whose Origin/Destination match the
/// request, so every test below passes the origin/destination that actually matches the
/// flight under test rather than relying on the old "always return the full schedule"
/// behavior. Default origin/destination (LHR/JFK) matches GA101.
///
/// Route-coverage fix (PO defect, 2026-07-07: "empty state most of the time"): the schedule
/// is now the 4 fixtures PLUS deterministic RouteScheduleGenerator flights (GA departures
/// 07:30 and 16:45, baseFare = round(60 + duration × 0.55, 2)) for every ordered airport
/// pair the fixtures don't cover — so every valid route returns results, while fixture
/// routes still return exactly their documented flights and prices. Sole exception: the
/// deliberate MAN&lt;-&gt;SYD no-direct-service pair (DEC-021, PO 2026-07-08), which keeps the
/// styled empty state demonstrable through a real search.
/// </summary>
public class GlobalAirProviderTests
{
    private static readonly string[] AirportCodes = { "LHR", "MAN", "JFK", "LAX", "DXB", "SYD" };

    /// <summary>The 4 ordered routes covered by GlobalAir's fixtures (Section 3.1) — each
    /// returns exactly its 1 documented flight; every other ordered pair returns exactly the
    /// 2 generated flights, except the DEC-021 no-direct-service pair below (0 flights).</summary>
    private static readonly HashSet<(string Origin, string Destination)> FixtureRoutes = new()
    {
        ("LHR", "JFK"), ("LHR", "DXB"), ("JFK", "LAX"), ("MAN", "LHR"),
    };

    /// <summary>DEC-021 (PO 2026-07-08): MAN&lt;-&gt;SYD deliberately has no direct service in
    /// either direction from either provider — the demo route for the real empty state.</summary>
    private static readonly HashSet<(string Origin, string Destination)> NoDirectServiceRoutes = new()
    {
        ("MAN", "SYD"), ("SYD", "MAN"),
    };

    private readonly GlobalAirProvider _provider = new();

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
    public void ProviderName_IsExactlyGlobalAir()
    {
        Assert.Equal("GlobalAir", _provider.ProviderName);
    }

    /// <summary>
    /// Route-filtering fix: only the schedule entry whose Origin/Destination exactly
    /// matches the requested route is returned, one flight per known GlobalAir route
    /// (feature-provider-aggregation.md Section 3.1's 4 fixed entries, each on a distinct
    /// route). Case-insensitivity is covered separately below.
    /// </summary>
    [Theory]
    [InlineData("LHR", "JFK", "GA101")]
    [InlineData("LHR", "DXB", "GA204")]
    [InlineData("JFK", "LAX", "GA309")]
    [InlineData("MAN", "LHR", "GA412")]
    public async Task SearchAsync_FiltersToRequestedRoute_ReturnsOnlyTheMatchingFixedFlight(
        string origin, string destination, string expectedFlightNumber)
    {
        var results = await _provider.SearchAsync(MakeRequest(origin: origin, destination: destination), CancellationToken.None);

        var flight = Assert.Single(results);
        Assert.Equal("GlobalAir", flight.Provider);
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
        Assert.Equal("GA101", flight.FlightNumber);
    }

    /// <summary>
    /// Route-coverage fix: a valid route with no fixture now returns the deterministic
    /// generated flights instead of an empty list. DXB->LHR is the reverse of the fixture
    /// LHR->DXB route (pair index 3 in RouteScheduleGenerator's table, duration 420):
    /// numbers GA514/GA515 (500 + 3×4 + reverse-direction 2, +1 for the second flight),
    /// departures at GlobalAir's generated times 07:30/16:45, baseFare
    /// round(60 + 420 × 0.55, 2) = 291.00, BR-001 price 291.00 × 1.15 = 334.65.
    /// </summary>
    [Fact]
    public async Task SearchAsync_GeneratedRoute_ReturnsExactDeterministicFlights()
    {
        var results = await _provider.SearchAsync(
            MakeRequest(departureDate: new DateOnly(2026, 8, 1), origin: "DXB", destination: "LHR"),
            CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.All(results, r =>
        {
            Assert.Equal("GlobalAir", r.Provider);
            Assert.Equal("DXB", r.Origin);
            Assert.Equal("LHR", r.Destination);
            Assert.Equal(420, r.DurationMinutes);
            Assert.Equal("Economy", r.CabinClass);
            Assert.Equal(291.00m, r.BaseFare);
            Assert.Equal(334.65m, r.PricePerPassenger);
        });

        Assert.Equal("GA514", results[0].FlightNumber);
        Assert.Equal(new DateTime(2026, 8, 1, 7, 30, 0, DateTimeKind.Utc), results[0].DepartureDateTime);
        Assert.Equal(new DateTime(2026, 8, 1, 14, 30, 0, DateTimeKind.Utc), results[0].ArrivalDateTime);

        Assert.Equal("GA515", results[1].FlightNumber);
        Assert.Equal(new DateTime(2026, 8, 1, 16, 45, 0, DateTimeKind.Utc), results[1].DepartureDateTime);
        Assert.Equal(new DateTime(2026, 8, 1, 23, 45, 0, DateTimeKind.Utc), results[1].ArrivalDateTime);
    }

    /// <summary>
    /// Route-coverage fix: generation is pure (no randomness, no clock reads) — two
    /// identical searches must return field-for-field identical results.
    /// </summary>
    [Fact]
    public async Task SearchAsync_GeneratedRoute_IsDeterministicAcrossRepeatedCalls()
    {
        var first = await _provider.SearchAsync(MakeRequest(origin: "SYD", destination: "JFK"), CancellationToken.None);
        var second = await _provider.SearchAsync(MakeRequest(origin: "SYD", destination: "JFK"), CancellationToken.None);

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
    /// fixed schedule gets NO generated additions — LHR->DXB still returns exactly GA204
    /// with its documented duration (450, not the generated table's 420) and unchanged
    /// pricing (base 300.00 -> 345.00 per BR-001). LHR->JFK's identical guarantee (exactly
    /// GA101 at 250.00 -> 287.50) is pinned by the route-filter and worked-example tests
    /// above.
    /// </summary>
    [Fact]
    public async Task SearchAsync_FixtureRoute_IsNotAugmentedByGeneratedFlights()
    {
        var results = await _provider.SearchAsync(MakeRequest(origin: "LHR", destination: "DXB"), CancellationToken.None);

        var flight = Assert.Single(results);
        Assert.Equal("GA204", flight.FlightNumber);
        Assert.Equal(450, flight.DurationMinutes);
        Assert.Equal(300.00m, flight.BaseFare);
        Assert.Equal(345.00m, flight.PricePerPassenger);
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
    [InlineData("GA101", "LHR", "JFK", "Economy", 250.00, 287.50)]
    [InlineData("GA101", "LHR", "JFK", "Business", 500.00, 575.00)]
    [InlineData("GA101", "LHR", "JFK", "First Class", 875.00, 1006.25)]
    [InlineData("GA412", "MAN", "LHR", "Economy", 80.00, 92.00)]
    public async Task SearchAsync_AppliesBR001PricingFormula_PerWorkedExamples(
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
        var economy = await _provider.SearchAsync(MakeRequest("Economy", origin: "JFK", destination: "LAX"), CancellationToken.None);
        var business = await _provider.SearchAsync(MakeRequest("Business", origin: "JFK", destination: "LAX"), CancellationToken.None);
        var first = await _provider.SearchAsync(MakeRequest("First Class", origin: "JFK", destination: "LAX"), CancellationToken.None);

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
        var results = await _provider.SearchAsync(
            MakeRequest(departureDate: new DateOnly(2026, 8, 1), origin: "LHR", destination: "DXB"), CancellationToken.None);
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
    [InlineData("GA101", "LHR", "JFK")]
    [InlineData("GA204", "LHR", "DXB")]
    [InlineData("GA412", "MAN", "LHR")]
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
    /// fare exactly like a fixture one (same values SearchAsync returns for GA514/GA515 on
    /// DXB->LHR: base 291.00, ×2.0 Business multiplier applied before BR-001).
    /// </summary>
    [Theory]
    [InlineData("GA514", "Economy", 291.00, 334.65)]
    [InlineData("GA515", "Business", 582.00, 669.30)]
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
        var resolved = _provider.TryResolveFare("GA999", "Economy", out var baseFare, out var pricePerPassenger, out var origin, out var destination);

        Assert.False(resolved);
        Assert.Equal(0m, baseFare);
        Assert.Equal(0m, pricePerPassenger);
        Assert.Null(origin);
        Assert.Null(destination);
    }

    [Fact]
    public void TryResolveFare_FlightNumberIsCaseSensitive_UnlikeAnywhereElseInThisSchedule()
    {
        // Ordinal (case-sensitive) match by design — FlightNumber is a server-defined,
        // fixed-format identifier (not user free text), so there is no legitimate reason
        // for a lowercase variant to resolve.
        var resolved = _provider.TryResolveFare("ga101", "Economy", out _, out _, out _, out _);

        Assert.False(resolved);
    }
}
