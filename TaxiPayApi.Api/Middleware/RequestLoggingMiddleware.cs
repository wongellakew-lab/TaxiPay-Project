using System.Diagnostics;

namespace TaxiPayApi.Api.Middlewares;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N")[..8];

        context.Response.Headers["X-Trace-Id"] = traceId;
        context.Response.Headers["X-Correlation-Id"] = correlationId;

        // Skip excessive logging for health check probes
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await _next(context);
            return;
        }

        _logger.LogInformation("--> HTTP {Method} {Path} [TraceId: {TraceId}, CorrelationId: {CorrelationId}]",
            context.Request.Method,
            context.Request.Path,
            traceId,
            correlationId);

        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            _logger.LogInformation("<-- HTTP {StatusCode} {Method} {Path} in {Elapsed}ms [TraceId: {TraceId}]",
                context.Response.StatusCode,
                context.Request.Method,
                context.Request.Path,
                sw.ElapsedMilliseconds,
                traceId);
        }
    }
}