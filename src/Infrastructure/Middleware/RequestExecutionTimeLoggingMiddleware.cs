using System.Diagnostics;

namespace DOJO2.Infrastructure.Middleware;

/// <summary>
/// Логує час виконання кожного HTTP запиту.
/// </summary>
public sealed class RequestExecutionTimeLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestExecutionTimeLoggingMiddleware> _logger;

    public RequestExecutionTimeLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestExecutionTimeLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        await _next(context);

        stopwatch.Stop();
        _logger.LogInformation(
            "HTTP Request execution time: Method={Method}, Path={Path}, StatusCode={StatusCode}, ElapsedMs={ElapsedMs}",
            context.Request.Method,
            context.Request.Path.Value,
            context.Response.StatusCode,
            stopwatch.ElapsedMilliseconds);
    }
}

public static class RequestExecutionTimeLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestExecutionTimeLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestExecutionTimeLoggingMiddleware>();
    }
}
