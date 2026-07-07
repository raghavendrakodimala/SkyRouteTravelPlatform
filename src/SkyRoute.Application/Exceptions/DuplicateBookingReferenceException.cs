namespace SkyRoute.Application.Exceptions;

/// <summary>
/// Signals that an IBookingStore.CreateAsync call could not persist a booking because its
/// BookingReference already exists in the store (BR-004/BR-008, code review finding CR-003).
/// This is thrown by the store implementation itself as the atomic source of truth for
/// reference uniqueness — IBookingStore.CreateAsync must perform an atomic add (e.g.
/// ConcurrentDictionary.TryAdd) and throw this exception on collision rather than silently
/// overwriting an existing record. BookingService.GenerateUniqueReferenceAsync catches this
/// exception to drive its bounded retry loop; it must never reach ApiExceptionMiddleware under
/// normal operation because the retry loop is expected to resolve the collision well within
/// MaxReferenceGenerationAttempts given the 36^6 reference keyspace.
/// </summary>
public sealed class DuplicateBookingReferenceException : Exception
{
    public string BookingReference { get; }

    public DuplicateBookingReferenceException(string bookingReference)
        : base($"A booking with reference '{bookingReference}' already exists.")
    {
        BookingReference = bookingReference;
    }
}
