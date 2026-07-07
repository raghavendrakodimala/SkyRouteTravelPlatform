using Microsoft.AspNetCore.Mvc.Testing;

namespace SkyRoute.Api.IntegrationTests;

/// <summary>
/// Shared WebApplicationFactory fixture for API integration tests (Microsoft.AspNetCore.Mvc.
/// Testing). Left with no ConfigureWebHost overrides so the default happy-path/validation
/// tests exercise the real DI registrations from Program.cs; per-test provider substitution
/// (e.g. the fault-isolation scenario) uses factory.WithWebHostBuilder(...) instead, to keep
/// this fixture clean.
/// </summary>
public sealed class SkyRouteApiFactory : WebApplicationFactory<Program>
{
    public HttpClient CreateHttpsClient() =>
        CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
}
