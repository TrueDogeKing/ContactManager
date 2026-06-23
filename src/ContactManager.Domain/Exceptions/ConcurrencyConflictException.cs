namespace ContactManager.Domain.Exceptions;

/// Sygnalize conflict optimistic concurrency – entity was modified.
/// by different process between read and write. Map on HTTP 409.
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
