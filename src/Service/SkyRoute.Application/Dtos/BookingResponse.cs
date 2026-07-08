namespace SkyRoute.Application.Dtos;

/// <summary>
/// POST /api/bookings 201 response body (FR-044, feature-booking-flow.md Section 4).
/// </summary>
public sealed class BookingResponse
{
    public required string BookingReference { get; init; }

    public required BookingFlightResponse Flight { get; init; }

    public required decimal TotalPrice { get; init; }

    public required IReadOnlyList<PassengerNameResponse> Passengers { get; init; }

    public required DateTime CreatedAtUtc { get; init; }
}
