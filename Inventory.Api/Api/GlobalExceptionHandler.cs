using Inventory.Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Api;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var inventoryException = exception as InventoryException;
        var badRequestException = exception as BadHttpRequestException;
        var statusCode = inventoryException?.StatusCode
            ?? (badRequestException is not null
                ? StatusCodes.Status400BadRequest
                : StatusCodes.Status500InternalServerError);
        var title = inventoryException?.Title
            ?? (badRequestException is not null ? "Invalid request" : "An unexpected error occurred.");
        var detail = inventoryException?.Message
            ?? (badRequestException is not null
                ? "The request contains an invalid or missing value."
                : "The server could not complete the request.");

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled error processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning("Request failed with status {StatusCode}: {Detail}",
                statusCode, detail);
        }

        httpContext.Response.StatusCode = statusCode;
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });

        return true;
    }
}
