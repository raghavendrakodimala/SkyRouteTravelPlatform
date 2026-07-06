namespace SkyRoute.Application.Contracts;

/// <summary>
/// The "flight" object nested in a POST /api/bookings 201 response (architecture-plan.md
/// Section 5, feature-booking-flow.md Section 4).
/// </summary>
public sealed class BookingFlightResponse
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
