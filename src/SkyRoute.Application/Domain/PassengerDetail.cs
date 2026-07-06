namespace SkyRoute.Application.Domain;

/// <summary>
/// One passenger's persisted booking record (BR-005). Plain POCO — no ORM/serialization
/// annotations (DP-PERSIST-002).
/// </summary>
public sealed class PassengerDetail
{
    public required string FullName { get; init; }

    public required string Email { get; init; }

    public required string DocumentType { get; init; }

    public required string DocumentNumber { get; init; }
}
