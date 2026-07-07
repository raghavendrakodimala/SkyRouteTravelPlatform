using System.Net;
using System.Net.Http.Json;
using SkyRoute.Api.IntegrationTests.TestDoubles;

namespace SkyRoute.Api.IntegrationTests;

/// <summary>
/// Global route prefix convention tests (PO directive 2026-07-08): the "api" prefix is applied
/// once by <c>GlobalRoutePrefixConvention</c> in Program.cs, never hardcoded per controller.
/// The prefixed routes must work (also pinned by every controller test) and the bare resource
/// segments must NOT be routable — proving the prefix comes from the convention, not from
/// duplicated controller attributes.
/// </summary>
public class RouteConventionTests : IClassFixture<SkyRouteApiFactory>
{
    private readonly SkyRouteApiFactory _factory;

    public RouteConventionTests(SkyRouteApiFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("/search")]
    [InlineData("/bookings")]
    [InlineData("/api/search")] // versionless — the URL-segment version is part of the route (PO directive 2026-07-08)
    [InlineData("/api/bookings")]
    public async Task BareResourceSegments_WithoutTheGlobalPrefix_AreNotRoutable(string path)
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(path, new { });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/search")]
    [InlineData("/api/v1/bookings")]
    public async Task PrefixedRoutes_AreRoutable_ViaTheGlobalConvention(string path)
    {
        var client = _factory.CreateClient();

        // An empty body reaches the controller and fails validation (400) — the route exists.
        var response = await client.PostAsJsonAsync(path, new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
