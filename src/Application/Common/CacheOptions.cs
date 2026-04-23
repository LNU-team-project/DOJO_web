using System.ComponentModel.DataAnnotations;

namespace DOJO2.Application.Common;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    [Range(10, 86400)]
    public int LeaderboardSeconds { get; set; } = 120;

    [Range(10, 86400)]
    public int AdminUsersSeconds { get; set; } = 180;
}