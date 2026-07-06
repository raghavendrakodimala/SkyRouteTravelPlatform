namespace SkyRoute.Application.Domain;

/// <summary>
/// Static airport reference record used for backend validation (FR-006, FR-057).
/// Plain POCO — no ORM, serialization, or schema annotations (DP-PERSIST-002, DP-PROTOCOL-002).
/// </summary>
public sealed class Airport
{
    public required string Code { get; init; }

    public required string City { get; init; }

    public required string Country { get; init; }

    public required string DisplayName { get; init; }
}
