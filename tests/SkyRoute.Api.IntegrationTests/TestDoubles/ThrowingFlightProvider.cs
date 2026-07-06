using SkyRoute.Application.Contracts;
using SkyRoute.Application.Domain;
using SkyRoute.Application.Interfaces;

namespace SkyRoute.Api.IntegrationTests.TestDoubles;

/// <summary>
/// Minimal throwing IFlightProvider test double for the integration-level provider
/// fault-isolation scenario (BR-007/FR-009/FR-050/FR-070, test-strategy.md Section 6). This
/// project does not reference SkyRoute.Application.Tests, so this ~15-line class is
/// duplicated here rather than adding a test-project-to-test-project reference.
/// </summary>
public sealed class ThrowingFlightProvider : IFlightProvider
{
    public string ProviderName { get; }

    private readonly Exception _exceptionToThrow;

    public ThrowingFlightProvider(string providerName, Exception exceptionToThrow)
    {
        ProviderName = providerName;
        _exceptionToThrow = exceptionToThrow;
    }

    public Task<IReadOnlyList<FlightResult>> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        throw _exceptionToThrow;
    }
}
