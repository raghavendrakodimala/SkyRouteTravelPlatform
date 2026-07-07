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

        var results = await sut.SearchAsync(MakeRequest(), CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.FlightNumber == "A1");
        Assert.Contains(results, r => r.FlightNumber == "B1");
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

        var results = await sut.SearchAsync(MakeRequest(), CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("GA101", results[0].FlightNumber);
        Assert.Equal("GlobalAir", results[0].Provider);
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
    public async Task SearchAsync_AllProvidersThrow_ReturnsEmptyResultSet_NoExceptionPropagates()
    {
        var providerA = new StubFlightProvider("ProviderA", exceptionToThrow: new Exception("fault A"));
        var providerB = new StubFlightProvider("ProviderB", exceptionToThrow: new Exception("fault B"));
        var logger = new CapturingLogger<FlightAggregatorService>();
        var sut = new FlightAggregatorService(new[] { providerA, providerB }, logger);

        var results = await sut.SearchAsync(MakeRequest(), CancellationToken.None);

        Assert.Empty(results);
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
    public async Task SearchAsync_NoProvidersRegistered_ReturnsEmptyResultSet()
    {
        var logger = new CapturingLogger<FlightAggregatorService>();
        var sut = new FlightAggregatorService(Array.Empty<StubFlightProvider>(), logger);

        var results = await sut.SearchAsync(MakeRequest(), CancellationToken.None);

        Assert.Empty(results);
    }
}
