using System.ComponentModel.DataAnnotations;

namespace DOJO2.Application.Common;

public sealed class AuthCookieOptions
{
    public const string SectionName = "AuthCookie";
    public const string DefaultBlockedNoticeCookieName = "dojo_blocked_notice";

    [Range(1, 168)]
    public int ExpireHours { get; set; } = 8;

    [Range(1, 60)]
    public int BlockedNoticeMinutes { get; set; } = 5;

    [Required]
    [MinLength(1)]
    public string BlockedNoticeCookieName { get; set; } = DefaultBlockedNoticeCookieName;
}


