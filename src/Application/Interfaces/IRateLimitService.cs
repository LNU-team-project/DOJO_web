using DOJO2.Application.Common;

namespace DOJO2.Application.Interfaces;

public interface IRateLimitService
{
    RateLimitDecision CheckRequest(string clientIpAddress, int maxRequestsPerMinute);
}

