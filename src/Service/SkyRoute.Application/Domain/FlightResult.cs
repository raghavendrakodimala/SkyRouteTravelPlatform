namespace SkyRoute.Application.Domain;

/// <summary>
/// A single flight offer returned by a provider (FR-010, FR-052). Plain POCO — no
/// ORM/serialization/schema annotations (DP-PERSIST-002, DP-PROTOCOL-002). Serializes to
/// camelCase JSON via the API's default System.Text.Json naming policy, not attributes.
/// </summary>
public sealed class FlightResult
{
    public required string Provider { get; init; }

    public required string FlightNumber { get; init; }

    public required string Origin { get; init; }

    public required string Destination { get; init; }

    public required DateTime DepartureDateTime { get; init; }

    public required DateTime ArrivalDateTime { get; init; }

    public required int DurationMinutes { get; init; }

    public required string CabinClass { get; init; }

    public required decimal BaseFare { get; init; }

    public required decimal PricePerPassenger { get; init; }
}
