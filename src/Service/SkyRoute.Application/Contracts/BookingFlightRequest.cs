namespace SkyRoute.Application.Contracts;

/// <summary>
/// The "flight" object nested in a POST /api/bookings request — a full flight-detail
/// snapshot carried from search results (AD-004), not an opaque flight identifier.
/// All fields are nullable so BookingRequestValidator can detect and report a missing
/// required field explicitly (feature-booking-flow.md Section 3/7).
/// </summary>
public sealed class BookingFlightRequest
{
    public string? Provider { get; set; }

    public string? FlightNumber { get; set; }

    public string? Origin { get; set; }

    public string? Destination { get; set; }

    public DateTime? DepartureDateTime { get; set; }

    public DateTime? ArrivalDateTime { get; set; }

    public int? DurationMinutes { get; set; }

    public string? CabinClass { get; set; }

    public decimal? BaseFare { get; set; }

    public decimal? PricePerPassenger { get; set; }
}
