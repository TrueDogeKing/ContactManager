namespace ContactManager.Domain.Exceptions;

/// Signals that a contact with the given email address already exists. Mapped to HTTP 409.
public class EmailConflictException : Exception
{
    public EmailConflictException(string message) : base(message)
    {
    }
}
