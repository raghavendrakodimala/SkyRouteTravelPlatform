namespace SkyRoute.Application.Contracts;

/// <summary>
/// One passenger record in a POST /api/bookings request (FR-040, feature-booking-flow.md Section 2).
/// </summary>
public sealed class PassengerRequest
{
    public string? FullName { get; set; }

    /// <summary>
    /// Required, whole number 0–120 (PO age feature 2026-07-08). Nullable so a missing
    /// "age" in the JSON body is detected by BookingRequestValidator as a field-level 400
    /// rather than silently defaulting to 0. Pure data capture — no business rule is bound
    /// to age (DEC-022, PO 2026-07-08).
    /// </summary>
    public int? Age { get; set; }

    public string? Email { get; set; }

    public string? DocumentType { get; set; }

    public string? DocumentNumber { get; set; }
}
