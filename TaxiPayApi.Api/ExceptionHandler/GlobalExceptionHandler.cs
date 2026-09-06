using System.Diagnostics;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TaxiPayApi.Api.ExceptionHandlers;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? httpContext.TraceIdentifier;

        var result = exception switch
        {
            ValidationException ve => (
                status: StatusCodes.Status400BadRequest,
                title: "Validation failed",
                detail: "One or more validation errors occurred.",
                errors: (IDictionary<string, string[]>?)ve.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray())),

            _ => (
                status: StatusCodes.Status500InternalServerError,
                title: "Server error",
                detail: $"An unexpected error occurred. Trace ID: {traceId}",
                errors: null)
        };

        var status = result.status;
        var title = result.title;
        var detail = result.detail;
        var errors = result.errors;

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception occurred while processing request {Path} [TraceId: {TraceId}]",
                httpContext.Request.Path, traceId);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path,
            Type = status == StatusCodes.Status400BadRequest
                ? "https://taxipay.local/errors/validation"
                : "https://taxipay.local/errors/internal_server_error"
        };

        problem.Extensions["traceId"] = traceId;
        if (errors is not null)
        {
            problem.Extensions["errors"] = errors;
        }

        httpContext.Response.StatusCode = status;
        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(problem, ct);

        return true;
    }
}