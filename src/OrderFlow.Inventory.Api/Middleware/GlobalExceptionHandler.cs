using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using OrderFlow.Inventory.Application.Common.Exceptions;
using OrderFlow.Inventory.Domain;

namespace OrderFlow.Inventory.Api.Middleware;

/// <summary>Maps domain and application exceptions onto RFC 7807 problem details.</summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            ValidationException validation => (StatusCodes.Status400BadRequest, "Validation failed",
                string.Join(" ", validation.Errors.Select(e => e.ErrorMessage))),
            NotFoundException notFound => (StatusCodes.Status404NotFound, "Not found", notFound.Message),
            DomainException domain => (StatusCodes.Status409Conflict, "Conflict", domain.Message),
            _ => (StatusCodes.Status500InternalServerError, "Server error", "An unexpected error occurred.")
        };

        if (status >= 500)
        {
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}
