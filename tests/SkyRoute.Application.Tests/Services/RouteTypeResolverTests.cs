using SkyRoute.Application.Data;
using SkyRoute.Application.Domain;
using SkyRoute.Application.Services;

namespace SkyRoute.Application.Tests.Services;

/// <summary>
/// Unit tests for RouteTypeResolver (BR-003, DP-016). Covers the worked examples in
/// feature-booking-flow.md Section 2.3.1 and the unknown-airport-code fallback documented in
/// HO-012A decision 7.
/// </summary>
public class RouteTypeResolverTests
{
    private readonly RouteTypeResolver _resolver = new(new AirportDataService());

    [Fact]
    public void Resolve_LhrToJfk_IsInternational()
    {
        // Worked example, feature-booking-flow.md Section 2.3.1: UK vs US.
        Assert.Equal(RouteType.International, _resolver.Resolve("LHR", "JFK"));
    }

    [Fact]
    public void Resolve_ManToLhr_IsDomestic()
    {
        // Worked example, feature-booking-flow.md Section 2.3.1: both UK.
        Assert.Equal(RouteType.Domestic, _resolver.Resolve("MAN", "LHR"));
    }

    [Fact]
    public void Resolve_DxbToSyd_IsInternational()
    {
        Assert.Equal(RouteType.International, _resolver.Resolve("DXB", "SYD"));
    }

    [Fact]
    public void Resolve_UnknownOriginCode_DefaultsToInternational()
    {
        // HO-012A decision 7: unrecognized airport code defaults to the stricter,
        // safer document-format requirement.
        Assert.Equal(RouteType.International, _resolver.Resolve("ZZZ", "LHR"));
    }

    [Fact]
    public void Resolve_NullOriginCode_DefaultsToInternational()
    {
        Assert.Equal(RouteType.International, _resolver.Resolve(null, "LHR"));
    }
}
