namespace DOJO2.Application.Common;

public sealed class RateLimitDecision
{
    private RateLimitDecision(bool allowed, TimeSpan? retryAfter, int remainingRequests)
    {
        Allowed = allowed;
        RetryAfter = retryAfter;
        RemainingRequests = remainingRequests;
    }

    public bool Allowed { get; }
    public TimeSpan? RetryAfter { get; }
    public int RemainingRequests { get; }

    public static RateLimitDecision Allow(int remainingRequests)
        => new(true, null, remainingRequests);

    public static RateLimitDecision Block(TimeSpan retryAfter)
        => new(false, retryAfter < TimeSpan.Zero ? TimeSpan.Zero : retryAfter, 0);
}

