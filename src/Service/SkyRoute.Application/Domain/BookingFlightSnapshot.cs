namespace SkyRoute.Application.Domain;

/// <summary>
/// The flight-detail snapshot persisted with a booking (AD-004: bookings carry a full
/// flight snapshot rather than an opaque flight identifier). Plain POCO — no ORM/serialization
/// annotations (DP-PERSIST-002).
/// </summary>
public sealed class BookingFlightSnapshot
{
    public required string Provider { get; init; }

    public required string FlightNumber { get; init; }

    public required string Origin { get; init; }

    public required string Destination { get; init; }

    public required DateTime DepartureDateTime { get; init; }

    public required DateTime ArrivalDateTime { get; init; }

    public required string CabinClass { get; init; }

    public required decimal PricePerPassenger { get; init; }
}
