namespace SkyRoute.Application.Dtos;

/// <summary>
/// Data-minimized passenger entry in a booking response — full name and age only
/// (feature-booking-flow.md Section 4 note; NFR-PRIV-001/002 spirit — email/document data
/// is still never echoed back; age added per the PO age feature 2026-07-08).
/// </summary>
public sealed class PassengerNameResponse
{
    public required string FullName { get; init; }

    public required int Age { get; init; }
}
