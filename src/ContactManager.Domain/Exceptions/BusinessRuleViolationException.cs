namespace ContactManager.Domain.Exceptions;

/// Signals that a request violates a domain rule (e.g. an invalid category/subcategory
/// combination). Mapped to HTTP 400.
public class BusinessRuleViolationException : Exception
{
    public BusinessRuleViolationException(string message)
        : base(message) { }
}
