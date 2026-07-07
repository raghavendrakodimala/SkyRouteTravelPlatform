using SkyRoute.Application.Contracts;
using SkyRoute.Application.Domain;
using SkyRoute.Application.Interfaces;

namespace SkyRoute.Application.Tests.TestDoubles;

/// <summary>
/// Test double implementing IFlightProvider (test-strategy.md Section 3: "a test double
/// (stub/fake) implementing IFlightProvider is substituted for one real provider"). Can be
/// configured to return a fixed result set or to throw, for exercising
/// FlightAggregatorService's fault-isolation behavior (BR-007) without touching the real
/// GlobalAirProvider/BudgetWingsProvider mock data.
/// </summary>
public sealed class StubFlightProvider : IFlightProvider
{
    private readonly IReadOnlyList<FlightResult> _results;
    private readonly Exception? _exceptionToThrow;

    public int InvocationCount { get; private set; }

    public string ProviderName { get; }

    public StubFlightProvider(string providerName, IReadOnlyList<FlightResult>? results = null, Exception? exceptionToThrow = null)
    {
        ProviderName = providerName;
        _results = results ?? Array.Empty<FlightResult>();
        _exceptionToThrow = exceptionToThrow;
    }

    public Task<IReadOnlyList<FlightResult>> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        InvocationCount++;

        if (_exceptionToThrow is not null)
        {
            throw _exceptionToThrow;
        }

        return Task.FromResult(_results);
    }

    /// <summary>
    /// This stub is only used for FlightAggregatorService/search-flow tests (test-strategy.md
    /// Section 3), never for booking-flow/SEC-001 fare-resolution tests, so it always reports
    /// "not found" rather than emulating a real provider's schedule lookup.
    /// </summary>
    public bool TryResolveFare(string flightNumber, string cabinClass, out decimal baseFare, out decimal pricePerPassenger)
    {
        baseFare = 0m;
        pricePerPassenger = 0m;
        return false;
    }

    public static FlightResult MakeResult(string provider, string flightNumber) => new()
    {
        Provider = provider,
        FlightNumber = flightNumber,
        Origin = "LHR",
        Destination = "JFK",
        DepartureDateTime = new DateTime(2026, 8, 1, 9, 0, 0, DateTimeKind.Utc),
        ArrivalDateTime = new DateTime(2026, 8, 1, 17, 0, 0, DateTimeKind.Utc),
        DurationMinutes = 480,
        CabinClass = "Economy",
        BaseFare = 100.00m,
        PricePerPassenger = 115.00m,
    };
}
