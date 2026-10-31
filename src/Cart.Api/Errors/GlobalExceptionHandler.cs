using Cart.Application.Exceptions;
using Cart.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Cart.Api.Errors;

internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ProblemDetails problem = exception switch
        {
            ValidationException validation => new ValidationProblemDetails(ToErrors(validation))
            {
                Status = StatusCodes.Status400BadRequest
            },
            CartItemNotFoundException or ProductNotFoundException =>
                Problem(StatusCodes.Status404NotFound, exception.Message),
            DomainException => Problem(StatusCodes.Status422UnprocessableEntity, exception.Message),
            ConcurrencyConflictException => Problem(StatusCodes.Status409Conflict, exception.Message),
            _ => Problem(StatusCodes.Status500InternalServerError, detail: null)
        };

        // The exception middleware does not log exceptions that a handler has handled so unexpected ones are logged here.
        if (problem.Status == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception");

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static ProblemDetails Problem(int status, string? detail) =>
        new() { Status = status, Detail = detail };

    private static Dictionary<string, string[]> ToErrors(ValidationException exception) =>
        exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
}
