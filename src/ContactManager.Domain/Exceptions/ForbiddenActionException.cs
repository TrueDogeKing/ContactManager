namespace ContactManager.Domain.Exceptions;

/// Signals that the authenticated caller is not allowed to perform the requested action
/// (e.g. changing another account's password). Mapped to HTTP 403.
public class ForbiddenActionException : Exception
{
    public ForbiddenActionException(string message) : base(message)
    {
    }
}
