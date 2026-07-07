using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using SkyRoute.Application.Contracts;
using SkyRoute.Application.Exceptions;
using SkyRoute.Application.Interfaces;
using SkyRoute.Application.Validation;

namespace SkyRoute.Api.Controllers;

/// <summary>
/// Thin POST /api/bookings controller (DP-003, DP-005). Runs structural validation
/// (BookingRequestValidator.ValidateStructure — architecture-plan.md Section 3.3 step 1)
/// before delegating to IBookingService, which performs the remaining steps (route-type
/// resolution, document validation, pricing, reference generation, persistence). No
/// business logic lives in this controller.
/// </summary>
[ApiController]
[ApiVersion("1.0")] // mapped to the default v1 version (PO directive 2026-07-08)
[Route("bookings")] // global "api" prefix applied by GlobalRoutePrefixConvention (Program.cs)
public sealed class BookingController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly BookingRequestValidator _validator;

    public BookingController(IBookingService bookingService, BookingRequestValidator validator)
    {
        _bookingService = bookingService;
        _validator = validator;
    }

    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] BookingRequest request, CancellationToken cancellationToken)
    {
        var structuralErrors = _validator.ValidateStructure(request);
        if (structuralErrors.Count > 0)
        {
            return ValidationProblem(structuralErrors.ToModelState());
        }

        try
        {
            var response = await _bookingService.CreateBookingAsync(request, cancellationToken);

            // Gap-fill BF-05: no GET /api/bookings/{reference} endpoint exists in this MVP
            // for a Location header to reference, so StatusCode(201, ...) is used directly
            // rather than CreatedAtAction(...)/Created(uri, ...).
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (BookingValidationException ex)
        {
            // A business-rule validation outcome (document/route-type mismatch), not an
            // unhandled exception (BR-011) — handled here, never reaching ApiExceptionMiddleware.
            return ValidationProblem(ex.Errors.ToModelState());
        }
    }
}
