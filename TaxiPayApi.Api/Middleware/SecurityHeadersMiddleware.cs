namespace TaxiPayApi.Api.Middlewares;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;

        // Prevent MIME type sniffing
        headers["X-Content-Type-Options"] = "nosniff";

        // Prevent clickjacking by denying iframe rendering
        headers["X-Frame-Options"] = "DENY";

        // Control referrer information sent in HTTP requests
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Modern browsers: disable legacy XSS filter which can introduce vulnerabilities
        headers["X-XSS-Protection"] = "0";

        // Feature & permission restrictions
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(self)";

        // Content Security Policy tailored for TaxiPay Angular client and SignalR WebSockets
        headers["Content-Security-Policy"] =
            "default-src 'self'; " +
            "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
            "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
            "font-src 'self' https://fonts.gstatic.com data:; " +
            "img-src 'self' data: https: blob:; " +
            "connect-src 'self' http://localhost:5000 https://localhost:5001 ws://localhost:5000 wss://localhost:5001 http://localhost:4200 ws://localhost:4200 wss://localhost:4200; " +
            "frame-ancestors 'none';";

        await _next(context);
    }
}
