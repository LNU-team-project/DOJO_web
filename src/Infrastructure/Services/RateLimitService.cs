using System.Collections.Concurrent;
using DOJO2.Application.Common;
using DOJO2.Application.Interfaces;

namespace DOJO2.Infrastructure.Services;

public sealed class RateLimitService : IRateLimitService
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private readonly ConcurrentDictionary<string, RateLimitBucket> _buckets = new(StringComparer.OrdinalIgnoreCase);
    private readonly IClock _clock;
    private readonly ILogger<RateLimitService> _logger;

    public RateLimitService(IClock clock, ILogger<RateLimitService> logger)
    {
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public RateLimitDecision CheckRequest(string clientIpAddress, int maxRequestsPerMinute)
    {
        if (string.IsNullOrWhiteSpace(clientIpAddress))
        {
            clientIpAddress = "unknown";
        }

        if (maxRequestsPerMinute <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRequestsPerMinute));
        }

        var bucket = _buckets.GetOrAdd(clientIpAddress.Trim(), static _ => new RateLimitBucket());
        var now = _clock.UtcNow;
        var threshold = now - Window;

        lock (bucket.SyncRoot)
        {
            while (bucket.RequestTimes.Count > 0 && bucket.RequestTimes.Peek() <= threshold)
            {
                bucket.RequestTimes.Dequeue();
            }

            if (bucket.RequestTimes.Count >= maxRequestsPerMinute)
            {
                var oldestRequest = bucket.RequestTimes.Peek();
                var retryAfter = oldestRequest.Add(Window) - now;
                _logger.LogWarning(
                    "Перевищено rate limit для IP {ClientIpAddress}. Ліміт: {MaxRequestsPerMinute}",
                    clientIpAddress,
                    maxRequestsPerMinute);

                return RateLimitDecision.Block(retryAfter);
            }

            bucket.RequestTimes.Enqueue(now);
            var remainingRequests = maxRequestsPerMinute - bucket.RequestTimes.Count;
            return RateLimitDecision.Allow(remainingRequests);
        }
    }

    private sealed class RateLimitBucket
    {
        public object SyncRoot { get; } = new();
        public Queue<DateTimeOffset> RequestTimes { get; } = new();
    }
}

