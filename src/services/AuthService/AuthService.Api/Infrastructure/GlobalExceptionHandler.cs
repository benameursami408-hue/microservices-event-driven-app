using AuthService.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Api.Infrastructure;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Response.Headers.TryGetValue(CorrelationIdMiddleware.HeaderName, out var value)
            ? value.ToString()
            : httpContext.TraceIdentifier;

        _logger.LogError(exception, "Unhandled exception. CorrelationId={CorrelationId}", correlationId);

        var problemDetails = new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Server error",
            Detail = "An unexpected error occurred. Contact support with the correlation id."
        };

        if (exception is CustomException customException)
        {
            problemDetails.Status = customException.StatusCode;
            problemDetails.Title = customException.StatusCode switch
            {
                StatusCodes.Status400BadRequest => "Bad Request",
                StatusCodes.Status401Unauthorized => "Unauthorized",
                StatusCodes.Status403Forbidden => "Forbidden",
                StatusCodes.Status404NotFound => "Not Found",
                StatusCodes.Status409Conflict => "Conflict",
                StatusCodes.Status422UnprocessableEntity => "Unprocessable Entity",
                StatusCodes.Status429TooManyRequests => "Too Many Requests",
                _ => customException.GetType().Name
            };
            problemDetails.Detail = customException.StatusCode >= 500 && !_environment.IsDevelopment()
                ? "An unexpected error occurred. Contact support with the correlation id."
                : customException.Message;

            if (customException.Errors is not null)
            {
                problemDetails.Extensions["errors"] = customException.Errors;
            }
        }
        else if (exception is UnauthorizedAccessException)
        {
            problemDetails.Status = StatusCodes.Status403Forbidden;
            problemDetails.Title = "Forbidden";
            problemDetails.Detail = "You are authenticated but you are not allowed to perform this action.";
        }
        else if (exception is InvalidOperationException)
        {
            problemDetails.Status = StatusCodes.Status400BadRequest;
            problemDetails.Title = "Invalid operation";
            problemDetails.Detail = exception.Message;
        }
        else if (_environment.IsDevelopment())
        {
            problemDetails.Detail = exception.Message;
        }

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;
        problemDetails.Extensions["correlationId"] = correlationId;

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
