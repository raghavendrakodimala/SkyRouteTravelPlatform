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

    /// <summary>
    /// Optional advisory field carried from a search result. AUD-032: BaseFare is validated
    /// when present (must be &gt; 0 and, once the flight is resolved, must equal the provider's
    /// re-derived base fare — see BookingRequestValidator), but it is NOT part of the persisted
    /// booking snapshot or the 201 response contract: PricePerPassenger is the single
    /// authoritative per-passenger price that is re-resolved, stored, and echoed back. Kept
    /// nullable/optional so a client that omits it is not forced to send a redundant value.
    /// </summary>
    public decimal? BaseFare { get; set; }

    public decimal? PricePerPassenger { get; set; }
}
