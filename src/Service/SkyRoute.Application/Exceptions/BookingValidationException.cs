namespace SkyRoute.Application.Exceptions;

/// <summary>
/// Signals a 400-worthy booking validation failure discovered inside BookingService after
/// server-side route-type resolution (feature-booking-flow.md Section 5, step 3) — e.g. a
/// document number/type mismatch against the authoritative resolved route type. This is a
/// plain BCL exception type (no Microsoft.AspNetCore.* reference, DP-PROTOCOL-001) carrying a
/// field-keyed error dictionary. BookingController catches this specific type and converts it
/// into a ValidationProblem response; it is a business-rule validation outcome, not an
/// unhandled exception, so it must never reach ApiExceptionMiddleware (BR-011).
/// </summary>
public sealed class BookingValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public BookingValidationException(IDictionary<string, string[]> errors)
        : base("Booking request failed document/route-type validation.")
    {
        Errors = errors;
    }
}
