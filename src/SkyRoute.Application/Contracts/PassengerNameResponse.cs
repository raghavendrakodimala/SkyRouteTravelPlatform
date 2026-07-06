namespace SkyRoute.Application.Contracts;

/// <summary>
/// Data-minimized passenger entry in a booking response — full name only
/// (feature-booking-flow.md Section 4 note; NFR-PRIV-001/002 spirit).
/// </summary>
public sealed class PassengerNameResponse
{
    public required string FullName { get; init; }
}
