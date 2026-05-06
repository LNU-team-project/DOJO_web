using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace DOJO2.Presentation.Hubs;

[Authorize]
public sealed class NotificationsHub : Hub
{
}

