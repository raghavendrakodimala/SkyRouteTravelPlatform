using System.Linq;
using System.Net;
using System.Net.Http.Json;
using SkyRoute.Application.Contracts;

namespace SkyRoute.Api.IntegrationTests.Middleware;

/// <summary>
/// Full-stack integration test for the SEC-003 (Phase 16 security review) baseline HTTP
/// security response header middleware registered in Program.cs. Exercised through
/// WebApplicationFactory&lt;Program&gt; against an existing endpoint (POST /api/search) so the
/// assertion covers the real pipeline ordering, not just the middleware delegate in isolation.
/// </summary>
public class SecurityHeadersTests : IClassFixture<SkyRouteApiFactory>
{
    private readonly SkyRouteApiFactory _factory;

    public SecurityHeadersTests(SkyRouteApiFactory factory)
    {
        _factory = factory;
    }

    private static SearchRequest MakeValidRequest() => new()
    {
        Origin = "LHR",
        Destination = "JFK",
        DepartureDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1),
        PassengerCount = 2,
        CabinClass = "Economy",
        TripType = "OneWay",
    };

    [Fact]
    public async Task AnyResponse_IncludesBaselineSecurityHeaders()
    {
        var client = _factory.CreateHttpsClient();

        var response = await client.PostAsJsonAsync("/api/search", MakeValidRequest());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").Single());
        Assert.Equal("no-referrer", response.Headers.GetValues("Referrer-Policy").Single());
    }
}
