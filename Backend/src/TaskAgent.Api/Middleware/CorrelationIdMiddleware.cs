namespace TaskAgent.Api.Middleware;

/// <summary>
/// Ensures every request has a correlation ID for tracing across services.
/// Reads X-Correlation-Id from request or generates a new one; adds it to HttpContext.Items and response headers.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-Id";
    public const string ItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var id = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrEmpty(id))
            id = Guid.NewGuid().ToString("N");

        context.Items[ItemKey] = id;
        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(HeaderName))
                context.Response.Headers.Append(HeaderName, id);
            return Task.CompletedTask;
        });

        await _next(context);
    }
}
