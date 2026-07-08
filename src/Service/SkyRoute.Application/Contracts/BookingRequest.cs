namespace SkyRoute.Application.Contracts;

/// <summary>
/// POST /api/bookings request body (architecture-plan.md Section 5, feature-booking-flow.md Section 3).
/// </summary>
public sealed class BookingRequest
{
    public BookingFlightRequest Flight { get; set; } = new();

    public int PassengerCount { get; set; }

    public List<PassengerRequest> Passengers { get; set; } = [];
}
