using SkyRoute.Application.Interfaces;
using SkyRoute.Application.Services;
using SkyRoute.Infrastructure.Providers;

namespace SkyRoute.Application.Tests.Services;

/// <summary>
/// Unit tests for FlightFareResolver (SEC-001, Phase 16 security review; BR-006,
/// NFR-DATA-002). Composed with the real GlobalAirProvider/BudgetWingsProvider (no mocking
/// library, per test-strategy.md's stub/fake approach) so these tests exercise the actual
/// authoritative fare re-resolution path BookingService relies on, not a stand-in.
/// </summary>
public class FlightFareResolverTests
{
    private static readonly IReadOnlyList<IFlightProvider> RealProviders = new IFlightProvider[]
    {
        new GlobalAirProvider(),
        new BudgetWingsProvider(),
    };

    private readonly FlightFareResolver _resolver = new(RealProviders);

    [Fact]
    public void TryResolveFare_KnownGlobalAirFlight_ReturnsTrueWithMatchingFareAndRoute()
    {
        var resolved = _resolver.TryResolveFare(
            "GlobalAir", "GA101", "Economy", out var baseFare, out var pricePerPassenger, out var origin, out var destination);

        Assert.True(resolved);
        Assert.Equal(250.00m, baseFare);
        Assert.Equal(287.50m, pricePerPassenger);
        // AUD-025/028/033: the resolver surfaces the flight's authoritative route.
        Assert.Equal("LHR", origin);
        Assert.Equal("JFK", destination);
    }

    [Fact]
    public void TryResolveFare_KnownBudgetWingsFlight_ReturnsTrueWithMatchingFareAndRoute()
    {
        var resolved = _resolver.TryResolveFare(
            "BudgetWings", "BW210", "Economy", out var baseFare, out var pricePerPassenger, out var origin, out var destination);

        Assert.True(resolved);
        Assert.Equal(220.00m, baseFare);
        Assert.Equal(198.00m, pricePerPassenger);
        Assert.Equal("LHR", origin);
        Assert.Equal("JFK", destination);
    }

    [Fact]
    public void TryResolveFare_UnknownProviderName_ReturnsFalse()
    {
        // A provider name that does not match any registered IFlightProvider at all — e.g. a
        // client submitting a fabricated provider string — must not silently resolve to some
        // default; there is no authoritative fare to compare against.
        var resolved = _resolver.TryResolveFare(
            "NotARealAirline", "GA101", "Economy", out var baseFare, out var pricePerPassenger, out var origin, out var destination);

        Assert.False(resolved);
        Assert.Equal(0m, baseFare);
        Assert.Equal(0m, pricePerPassenger);
        Assert.Null(origin);
        Assert.Null(destination);
    }

    [Fact]
    public void TryResolveFare_FlightNumberBelongsToADifferentProvider_ReturnsFalse()
    {
        // GA101 is a GlobalAir flight number; asking BudgetWings to resolve it must fail
        // rather than accidentally matching a coincidental cross-provider collision.
        var resolved = _resolver.TryResolveFare("BudgetWings", "GA101", "Economy", out _, out _, out _, out _);

        Assert.False(resolved);
    }

    [Theory]
    [InlineData(null, "GA101", "Economy")]
    [InlineData("GlobalAir", null, "Economy")]
    [InlineData("GlobalAir", "GA101", null)]
    [InlineData("", "GA101", "Economy")]
    [InlineData("GlobalAir", "", "Economy")]
    [InlineData("GlobalAir", "GA101", "")]
    public void TryResolveFare_MissingIdentifyingField_ReturnsFalseWithoutThrowing(
        string? providerName, string? flightNumber, string? cabinClass)
    {
        var resolved = _resolver.TryResolveFare(
            providerName, flightNumber, cabinClass, out var baseFare, out var pricePerPassenger, out var origin, out var destination);

        Assert.False(resolved);
        Assert.Equal(0m, baseFare);
        Assert.Equal(0m, pricePerPassenger);
        Assert.Null(origin);
        Assert.Null(destination);
    }
}
