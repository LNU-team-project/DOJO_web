using System;
using System.Collections.Generic;

namespace DOJO2.Presentation.ViewModels;

public class AdminUserListItemViewModel
{
    public int Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public int Level { get; set; }
    public int ExpPoints { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminUsersPageViewModel
{
    public string Search { get; set; } = string.Empty;
    public List<AdminUserListItemViewModel> Users { get; set; } = new();
}
