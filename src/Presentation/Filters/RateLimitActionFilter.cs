using DOJO2.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DOJO2.Presentation.Filters;

public sealed class RateLimitActionFilter : IActionFilter
{
    private const string HomeControllerName = "Home";
    private const string ErrorActionName = "Error";

    private readonly IRateLimitService _rateLimitService;
    private readonly ILogger<RateLimitActionFilter> _logger;
    private readonly int _maxRequestsPerMinute;

    public RateLimitActionFilter(
        IRateLimitService rateLimitService,
        ILogger<RateLimitActionFilter> logger,
        int maxRequestsPerMinute)
    {
        _rateLimitService = rateLimitService ?? throw new ArgumentNullException(nameof(rateLimitService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _maxRequestsPerMinute = maxRequestsPerMinute > 0
            ? maxRequestsPerMinute
            : throw new ArgumentOutOfRangeException(nameof(maxRequestsPerMinute));
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        var clientIpAddress = ResolveClientIpAddress(context.HttpContext);
        var decision = _rateLimitService.CheckRequest(clientIpAddress, _maxRequestsPerMinute);

        if (decision.Allowed)
        {
            return;
        }

        var retryAfterSeconds = decision.RetryAfter.HasValue
            ? Math.Max(1, (int)Math.Ceiling(decision.RetryAfter.Value.TotalSeconds))
            : 60;

        _logger.LogWarning(
            "Блокування запиту через перевищення rate limit. IP: {ClientIpAddress}, RetryAfter: {RetryAfterSeconds}s",
            clientIpAddress,
            retryAfterSeconds);

        context.Result = new RedirectToActionResult(
            ErrorActionName,
            HomeControllerName,
            new { message = $"Перевищено ліміт запитів. Спробуйте ще раз через {retryAfterSeconds} с." });
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
    }

    private static string ResolveClientIpAddress(HttpContext httpContext)
    {
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RateLimitAttribute : TypeFilterAttribute
{
    public RateLimitAttribute(int maxRequestsPerMinute)
        : base(typeof(RateLimitActionFilter))
    {
        Arguments = new object[] { maxRequestsPerMinute };
    }
}

