namespace ContactManager.Domain.Exceptions;

/// Signals an optimistic concurrency conflict: the entity was modified by another
/// process between read and write. Mapped to HTTP 409.
public class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message) : base(message)
    {
    }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
