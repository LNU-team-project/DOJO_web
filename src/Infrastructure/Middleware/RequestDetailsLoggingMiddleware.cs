using System.Security.Claims;
using System.Text;

namespace DOJO2.Infrastructure.Middleware;

/// <summary>
/// Логує ключову інформацію про HTTP запит: метод, URL, IP, заголовки, тіло та ID користувача.
/// </summary>
public sealed class RequestDetailsLoggingMiddleware
{
    private const int MaxBodyLogLength = 4096;

    private static readonly HashSet<string> SensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization",
        "Cookie",
        "Set-Cookie",
        "X-Api-Key"
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<RequestDetailsLoggingMiddleware> _logger;

    public RequestDetailsLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestDetailsLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var method = context.Request.Method;
        var url = $"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}";
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous";
        var headers = BuildHeadersSnapshot(context.Request.Headers);
        var requestBody = await ReadRequestBodyAsync(context.Request);

        _logger.LogInformation(
            "HTTP Request details: Method={Method}, Url={Url}, IpAddress={IpAddress}, UserId={UserId}, Headers={Headers}, Body={Body}",
            method,
            url,
            ipAddress,
            userId,
            headers,
            requestBody);

        await _next(context);
    }

    private static Dictionary<string, string> BuildHeadersSnapshot(IHeaderDictionary requestHeaders)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var header in requestHeaders)
        {
            headers[header.Key] = SensitiveHeaders.Contains(header.Key)
                ? "[REDACTED]"
                : string.Join(';', header.Value.ToArray());
        }

        return headers;
    }

    private static async Task<string> ReadRequestBodyAsync(HttpRequest request)
    {
        if (request.ContentLength.GetValueOrDefault() == 0 || request.Body == Stream.Null)
        {
            return "[EMPTY]";
        }

        request.EnableBuffering();
        request.Body.Position = 0;

        using var reader = new StreamReader(
            request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true);

        var bodyText = await reader.ReadToEndAsync();
        request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(bodyText))
        {
            return "[EMPTY]";
        }

        if (bodyText.Length <= MaxBodyLogLength)
        {
            return bodyText;
        }

        return string.Concat(
            bodyText.AsSpan(0, MaxBodyLogLength),
            "... [TRUNCATED]");
    }
}

public static class RequestDetailsLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseRequestDetailsLogging(this IApplicationBuilder app)
    {
        return app.UseMiddleware<RequestDetailsLoggingMiddleware>();
    }
}
