using Microsoft.Extensions.Logging;
using SkyRoute.Application.Contracts;
using SkyRoute.Application.Services;
using SkyRoute.Application.Tests.TestDoubles;

namespace SkyRoute.Application.Tests.Services;

/// <summary>
/// Unit tests for FlightAggregatorService (test-strategy.md Section 1.1 and Section 6 —
/// the required automated BR-007/FR-009/FR-050/NFR-TEST-006/NFR-AVAIL-002 provider
/// fault-isolation scenario at the unit level).
/// </summary>
public class FlightAggregatorServiceTests
{
    private static SearchRequest MakeRequest() => new()
    {
        Origin = "LHR",
        Destination = "JFK",
        DepartureDate = new DateOnly(2026, 8, 1),
        PassengerCount = 2,
        CabinClass = "Economy",
        TripType = "OneWay",
    };

    [Fact]
    public async Task SearchAsync_BothProvidersSucceed_MergesAllResults()
    {
        var providerA = new StubFlightProvider("ProviderA", new[] { StubFlightProvider.MakeResult("ProviderA", "A1") });
        var providerB = new StubFlightProvider("ProviderB", new[] { StubFlightProvider.MakeResult("ProviderB", "B1") });
        var logger = new CapturingLogger<FlightAggregatorService>();
        var sut = new FlightAggregatorService(new[] { providerA, providerB }, logger);

        var result = await sut.SearchAsync(MakeRequest(), CancellationToken.None);

        Assert.False(result.AllProvidersFailed);
        Assert.Equal(2, result.Flights.Count);
        Assert.Contains(result.Flights, r => r.FlightNumber == "A1");
        Assert.Contains(result.Flights, r => r.FlightNumber == "B1");
    }

    [Fact]
    public async Task SearchAsync_OneProviderThrows_ReturnsOnlySurvivingProviderResults_NoExceptionPropagates()
    {
        // BR-007/FR-009/FR-050/US-007-AC4 scenario per test-strategy.md Section 6.
        var survivingProvider = new StubFlightProvider(
            "GlobalAir", new[] { StubFlightProvider.MakeResult("GlobalAir", "GA101") });
        var throwingProvider = new StubFlightProvider(
            "BudgetWings", exceptionToThrow: new InvalidOperationException("Simulated BudgetWings internal fault"));
        var logger = new CapturingLogger<FlightAggregatorService>();
        var sut = new FlightAggregatorService(new[] { survivingProvider, throwingProvider }, logger);

        var result = await sut.SearchAsync(MakeRequest(), CancellationToken.None);

        // AUD-038: partial failure preserves fault isolation — NOT a total outage.
        Assert.False(result.AllProvidersFailed);
        Assert.Single(result.Flights);
        Assert.Equal("GA101", result.Flights[0].FlightNumber);
        Assert.Equal("GlobalAir", result.Flights[0].Provider);
    }

    [Fact]
    public async Task SearchAsync_OneProviderThrows_LogsWarningWithProviderNameAndException()
    {
        var survivingProvider = new StubFlightProvider("GlobalAir", new[] { StubFlightProvider.MakeResult("GlobalAir", "GA101") });
        var exception = new InvalidOperationException("Simulated BudgetWings internal fault");
        var throwingProvider = new StubFlightProvider("BudgetWings", exceptionToThrow: exception);
        var logger = new CapturingLogger<FlightAggregatorService>();
        var sut = new FlightAggregatorService(new[] { survivingProvider, throwingProvider }, logger);

        await sut.SearchAsync(MakeRequest(), CancellationToken.None);

        var warning = Assert.Single(logger.Entries, e => e.Level == LogLevel.Warning);
        Assert.Contains("BudgetWings", warning.Message);
        Assert.Same(exception, warning.Exception);
    }

    [Fact]
    public async Task SearchAsync_AllProvidersThrow_ReturnsEmptyResultSet_AndSignalsTotalOutage_NoExceptionPropagates()
    {
        // AUD-038: every registered provider failed. No exception propagates (fault isolation
        // still holds), but the outcome now signals a TOTAL outage so the controller returns 503
        // rather than an indistinguishable 200 [] that a healthy no-match would also produce.
        var providerA = new StubFlightProvider("ProviderA", exceptionToThrow: new Exception("fault A"));
        var providerB = new StubFlightProvider("ProviderB", exceptionToThrow: new Exception("fault B"));
        var logger = new CapturingLogger<FlightAggregatorService>();
        var sut = new FlightAggregatorService(new[] { providerA, providerB }, logger);

        var result = await sut.SearchAsync(MakeRequest(), CancellationToken.None);

        Assert.Empty(result.Flights);
        Assert.True(result.AllProvidersFailed);
    }

    [Fact]
    public async Task SearchAsync_AllProvidersSucceedButReturnNoFlights_IsNotATotalOutage()
    {
        // AUD-038: healthy providers that simply have no matching flights (e.g. the DEC-021
        // no-service route) is a genuine no-match — 200 [], NOT a 503. AllProvidersFailed stays
        // false so the two cases are distinguishable.
        var providerA = new StubFlightProvider("ProviderA");
        var providerB = new StubFlightProvider("ProviderB");
        var logger = new CapturingLogger<FlightAggregatorService>();
        var sut = new FlightAggregatorService(new[] { providerA, providerB }, logger);

        var result = await sut.SearchAsync(MakeRequest(), CancellationToken.None);

        Assert.Empty(result.Flights);
        Assert.False(result.AllProvidersFailed);
    }

    [Fact]
    public async Task SearchAsync_InvokesEveryRegisteredProviderExactlyOnce()
    {
        // FR-049 — concurrent invocation of every registered provider (asserted here via
        // call-count, since Task.WhenAll's true concurrency is an implementation detail not
        // independently observable without timing instrumentation).
        var providerA = new StubFlightProvider("ProviderA", new[] { StubFlightProvider.MakeResult("ProviderA", "A1") });
        var providerB = new StubFlightProvider("ProviderB", new[] { StubFlightProvider.MakeResult("ProviderB", "B1") });
        var providerC = new StubFlightProvider("ProviderC", exceptionToThrow: new Exception("fault C"));
        var logger = new CapturingLogger<FlightAggregatorService>();
        var sut = new FlightAggregatorService(new[] { providerA, providerB, providerC }, logger);

        await sut.SearchAsync(MakeRequest(), CancellationToken.None);

        Assert.Equal(1, providerA.InvocationCount);
        Assert.Equal(1, providerB.InvocationCount);
        Assert.Equal(1, providerC.InvocationCount);
    }

    [Fact]
    public async Task SearchAsync_NoProvidersRegistered_ReturnsEmptyResultSet_AndIsNotATotalOutage()
    {
        // AUD-038: zero providers registered is a configuration concern surfaced by the health
        // check, not a runtime outage — so it is 200 [] (AllProvidersFailed false), not 503.
        var logger = new CapturingLogger<FlightAggregatorService>();
        var sut = new FlightAggregatorService(Array.Empty<StubFlightProvider>(), logger);

        var result = await sut.SearchAsync(MakeRequest(), CancellationToken.None);

        Assert.Empty(result.Flights);
        Assert.False(result.AllProvidersFailed);
    }
}
