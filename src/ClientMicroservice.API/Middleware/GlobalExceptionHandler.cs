using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ClientMicroservice.Application.Common.Exceptions;

namespace ClientMicroservice.API.Middleware;

internal sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken ct)
    {
        logger.LogError(exception, "Unhandled exception: {Message}", exception.Message);

        var (statusCode, title) = exception switch
        {
            AppValidationException => (StatusCodes.Status422UnprocessableEntity, "Validation Error"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        ProblemDetails problem;
        if (exception is AppValidationException validationEx)
        {
            problem = new ValidationProblemDetails(validationEx.Errors)
            {
                Status = statusCode,
                Title = title,
                Type = "https://tools.ietf.org/html/rfc4918#section-11.2"
            };
        }
        else
        {
            problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = exception.Message
            };
        }

        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}
