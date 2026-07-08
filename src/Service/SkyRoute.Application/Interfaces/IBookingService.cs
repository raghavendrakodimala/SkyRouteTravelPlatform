using SkyRoute.Application.Dtos;

namespace SkyRoute.Application.Interfaces;

/// <summary>
/// The contract between the booking API controller and booking business logic
/// (DP-003, FR-046). The controller must not contain booking business logic directly.
/// No ASP.NET Core / HTTP-infrastructure type appears in this signature (DP-PROTOCOL-001).
/// </summary>
public interface IBookingService
{
    Task<BookingResponse> CreateBookingAsync(BookingRequest request, CancellationToken cancellationToken);
}
