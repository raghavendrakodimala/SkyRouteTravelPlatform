namespace SkyRoute.Application.Contracts;

/// <summary>
/// One passenger record in a POST /api/bookings request (FR-040, feature-booking-flow.md Section 2).
/// </summary>
public sealed class PassengerRequest
{
    public string? FullName { get; set; }

    public string? Email { get; set; }

    public string? DocumentType { get; set; }

    public string? DocumentNumber { get; set; }
}
