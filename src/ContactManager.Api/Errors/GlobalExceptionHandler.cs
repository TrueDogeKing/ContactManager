using ContactManager.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ContactManager.Api.Errors;

/// Maps unhandled exceptions to consistent ProblemDetails responses. Domain exceptions map to
/// their dedicated status codes; anything else becomes a 500 without leaking internal details.
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    /// Creates the handler with dependencies.
    public GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = Map(exception);

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception processing {Method} {Path}.",
                httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail
            }
        });
    }

    /// Resolves the status code, title and safe detail for a given exception.
    private static (int StatusCode, string Title, string? Detail) Map(Exception exception) =>
        exception switch
        {
            BusinessRuleViolationException => (StatusCodes.Status400BadRequest, "Bad Request", exception.Message),
            EmailConflictException => (StatusCodes.Status409Conflict, "Conflict", exception.Message),
            ConcurrencyConflictException => (StatusCodes.Status409Conflict, "Conflict", exception.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null)
        };
}
