using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TaxiPayApi.Application.Common.Telemetry;

namespace TaxiPayApi.Api.Extensions;

public static class RateLimitingExtensions
{
    public const string GlobalPolicy = "global-tier-policy";
    public const string AuthStrictPolicy = "auth-strict";
    public const string FinancialStrictPolicy = "financial-strict";
    public const string SearchStrictPolicy = "search-strict";

    public static IServiceCollection AddTaxiPayRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, token) =>
            {
                var httpContext = context.HttpContext;
                httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                httpContext.Response.ContentType = "application/problem+json";

                var retryAfterSeconds = 60;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfterSpan))
                {
                    retryAfterSeconds = (int)Math.Ceiling(retryAfterSpan.TotalSeconds);
                }

                httpContext.Response.Headers.RetryAfter = retryAfterSeconds.ToString(CultureInfo.InvariantCulture);

                var tier = GetUserTier(httpContext.User);
                TaxiPayDiagnostics.RateLimitRejectedTotal.Add(1,
                    new KeyValuePair<string, object?>("tier", tier),
                    new KeyValuePair<string, object?>("path", httpContext.Request.Path.Value ?? "unknown"));

                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too Many Requests",
                    Detail = "Too many requests. Please try again shortly.",
                    Instance = httpContext.Request.Path,
                    Type = "https://taxipay.local/errors/rate_limit_exceeded"
                };

                problem.Extensions["retryAfterSeconds"] = retryAfterSeconds;
                await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken: token);
            };

            // Global Tier-Aware Partitioned Limiter
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var user = httpContext.User;
                var isAuthenticated = user.Identity?.IsAuthenticated == true;
                var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

                if (isAuthenticated && !string.IsNullOrEmpty(userId))
                {
                    var tier = GetUserTier(user);
                    return tier switch
                    {
                        "Admin" => RateLimitPartition.GetSlidingWindowLimiter(
                            $"user:{userId}:admin",
                            _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = 100,
                                Window = TimeSpan.FromMinutes(1),
                                SegmentsPerWindow = 4,
                                QueueLimit = 10,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                            }),
                        "Driver" => RateLimitPartition.GetSlidingWindowLimiter(
                            $"user:{userId}:driver",
                            _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = 60,
                                Window = TimeSpan.FromMinutes(1),
                                SegmentsPerWindow = 4,
                                QueueLimit = 5,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                            }),
                        _ => RateLimitPartition.GetSlidingWindowLimiter(
                            $"user:{userId}:passenger",
                            _ => new SlidingWindowRateLimiterOptions
                            {
                                PermitLimit = 30,
                                Window = TimeSpan.FromMinutes(1),
                                SegmentsPerWindow = 4,
                                QueueLimit = 2,
                                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                            })
                    };
                }

                // Anonymous partition by Client IP
                var clientIp = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
                return RateLimitPartition.GetSlidingWindowLimiter(
                    $"anon:{clientIp}",
                    _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 15,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 4,
                        QueueLimit = 0
                    });
            });

            // Strict Auth Policy (e.g. login, register)
            options.AddPolicy(AuthStrictPolicy, httpContext =>
            {
                var key = httpContext.User.Identity?.IsAuthenticated == true
                    ? $"auth:{httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)}"
                    : $"auth:{httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"}";

                return RateLimitPartition.GetFixedWindowLimiter(key, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });

            // Financial Operations Policy (payments, top-up)
            options.AddPolicy(FinancialStrictPolicy, httpContext =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "anon";

                return RateLimitPartition.GetFixedWindowLimiter($"fin:{userId}", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });

            // Search Operations Policy (driver lookup / search)
            options.AddPolicy(SearchStrictPolicy, httpContext =>
            {
                var key = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "search-anon";

                return RateLimitPartition.GetFixedWindowLimiter($"search:{key}", _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1),
                    QueueLimit = 0
                });
            });
        });

        return services;
    }

    private static string GetUserTier(ClaimsPrincipal user)
    {
        if (user.IsInRole("Admin") || user.IsInRole("admin")) return "Admin";
        if (user.IsInRole("Driver") || user.IsInRole("driver")) return "Driver";
        if (user.IsInRole("Passenger") || user.IsInRole("passenger")) return "Passenger";
        return user.Identity?.IsAuthenticated == true ? "Passenger" : "Anonymous";
    }
}
