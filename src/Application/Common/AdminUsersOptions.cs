using System.ComponentModel.DataAnnotations;

namespace DOJO2.Application.Common;

public sealed class AdminUsersOptions
{
    public const string SectionName = "AdminUsers";

    [Range(1, 1000)]
    public int MaxUsersForAdminPage { get; set; } = 200;

    [Range(0, 10)]
    public int MinSearchLength { get; set; } = 2;

    [Range(1, 200)]
    public int LockoutYears { get; set; } = 100;
}

