using System.Diagnostics;

namespace TaskAgent.Api.Middleware;

/// <summary>
/// Logs HTTP request method, path, status code, and duration. Does not log request/response bodies or auth headers.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private static readonly PathString[] SkipPaths = [
        "/health",
        "/health/ready",
        "/health/live",
        "/swagger",
        "/favicon.ico"
    ];

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path + context.Request.QueryString;
        var correlationId = context.Items[CorrelationIdMiddleware.ItemKey] as string ?? "-";

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            // Log at Information for 2xx, Warning for 4xx, Error for 5xx
            if (statusCode >= 500)
                _logger.LogError("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms (CorrelationId: {CorrelationId})",
                    method, path, statusCode, stopwatch.ElapsedMilliseconds, correlationId);
            else if (statusCode >= 400)
                _logger.LogWarning("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms (CorrelationId: {CorrelationId})",
                    method, path, statusCode, stopwatch.ElapsedMilliseconds, correlationId);
            else
                _logger.LogInformation("HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs}ms (CorrelationId: {CorrelationId})",
                    method, path, statusCode, stopwatch.ElapsedMilliseconds, correlationId);
        }
    }

    private static bool ShouldSkip(PathString path)
    {
        foreach (var skip in SkipPaths)
        {
            if (path.StartsWithSegments(skip, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
