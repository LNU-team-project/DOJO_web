using System.Collections.Concurrent;
using System.Globalization;
using DOJO2.Application.Interfaces;
using DOJO2.Infrastructure.Data;
using DOJO2.Presentation.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace DOJO2.Infrastructure.BackgroundServices;

public sealed class NotificationBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IClock _clock;
    private readonly ILogger<NotificationBackgroundService> _logger;
    private readonly ConcurrentDictionary<string, int> _knownFriendRequestRecipients = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, int> _knownPlanReminderRecipients = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<int, DateOnly> _knownStreakWarnings = new();
    private readonly SemaphoreSlim _primeLock = new(1, 1);
    private bool _isPrimed;

    public NotificationBackgroundService(
        IServiceScopeFactory scopeFactory,
        IClock clock,
        ILogger<NotificationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Фоновий сервіс realtime-нотифікацій запущено");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка під час перевірки подій для нотифікацій");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<NotificationsHub>>();

        var now = _clock.UtcNow.UtcDateTime;
        var today = DateOnly.FromDateTime(now);
        var tomorrow = today.AddDays(1);

        await EnsurePrimedAsync(context, today, tomorrow, cancellationToken);

        var affectedUsers = new HashSet<int>();
        affectedUsers.UnionWith(await DetectFriendRequestChangesAsync(context, cancellationToken));
        affectedUsers.UnionWith(await DetectPlanReminderChangesAsync(context, tomorrow, cancellationToken));
        affectedUsers.UnionWith(await DetectStreakWarningChangesAsync(context, today, cancellationToken));

        foreach (var userId in affectedUsers)
        {
            await PushDashboardNotificationsAsync(notificationService, hubContext, userId, now, cancellationToken);
        }
    }

    private async Task EnsurePrimedAsync(AppDbContext context, DateOnly today, DateOnly tomorrow, CancellationToken cancellationToken)
    {
        if (_isPrimed)
        {
            return;
        }

        await _primeLock.WaitAsync(cancellationToken);
        try
        {
            if (_isPrimed)
            {
                return;
            }

            await SeedFriendRequestKeysAsync(context, cancellationToken);
            await SeedPlanReminderKeysAsync(context, tomorrow, cancellationToken);
            await SeedStreakWarningStateAsync(context, today, cancellationToken);
            _isPrimed = true;
        }
        finally
        {
            _primeLock.Release();
        }
    }

    private async Task SeedFriendRequestKeysAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var requests = await context.FriendRequests.AsNoTracking()
            .Select(request => new
            {
                request.Id,
                request.ReceiverUserId
            })
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            _knownFriendRequestRecipients.TryAdd(BuildFriendRequestKey(request.Id), request.ReceiverUserId);
        }
    }

    private async Task SeedPlanReminderKeysAsync(AppDbContext context, DateOnly tomorrow, CancellationToken cancellationToken)
    {
        var tomorrowDate = tomorrow.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).Date;
        var plansDueTomorrow = await context.Tasks.AsNoTracking()
            .Where(task => task.IsPlan
                && !task.IsCompleted
                && task.ScheduledAt.HasValue
                && task.ScheduledAt.Value.Date == tomorrowDate)
            .Select(task => new
            {
                task.Id,
                task.UserId,
                ScheduledAt = task.ScheduledAt!.Value
            })
            .ToListAsync(cancellationToken);

        foreach (var plan in plansDueTomorrow)
        {
            _knownPlanReminderRecipients.TryAdd(BuildPlanReminderKey(plan.Id, plan.ScheduledAt), plan.UserId);
        }
    }

    private async Task SeedStreakWarningStateAsync(AppDbContext context, DateOnly today, CancellationToken cancellationToken)
    {
        var usersAtRisk = await context.Users.AsNoTracking()
            .Where(user => user.CurrentStreak > 0
                && user.LastCompletionDate.HasValue
                && today.DayNumber - user.LastCompletionDate.Value.DayNumber > 0)
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);

        foreach (var userId in usersAtRisk)
        {
            _knownStreakWarnings.TryAdd(userId, today);
        }
    }

    private async Task<HashSet<int>> DetectFriendRequestChangesAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var affectedUsers = new HashSet<int>();
        var requests = await context.FriendRequests.AsNoTracking()
            .Select(request => new
            {
                request.Id,
                request.ReceiverUserId
            })
            .ToListAsync(cancellationToken);

        var currentRecipients = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var request in requests)
        {
            currentRecipients[BuildFriendRequestKey(request.Id)] = request.ReceiverUserId;
        }

        foreach (var (key, receiverUserId) in currentRecipients)
        {
            if (!_knownFriendRequestRecipients.TryGetValue(key, out var knownReceiverUserId) || knownReceiverUserId != receiverUserId)
            {
                affectedUsers.Add(receiverUserId);
            }
        }

        foreach (var (key, receiverUserId) in _knownFriendRequestRecipients)
        {
            if (!currentRecipients.ContainsKey(key))
            {
                affectedUsers.Add(receiverUserId);
            }
        }

        _knownFriendRequestRecipients.Clear();
        foreach (var (key, receiverUserId) in currentRecipients)
        {
            _knownFriendRequestRecipients[key] = receiverUserId;
        }

        return affectedUsers;
    }

    private async Task<HashSet<int>> DetectPlanReminderChangesAsync(AppDbContext context, DateOnly tomorrow, CancellationToken cancellationToken)
    {
        var affectedUsers = new HashSet<int>();
        var tomorrowDate = tomorrow.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc).Date;
        var plansDueTomorrow = await context.Tasks.AsNoTracking()
            .Where(task => task.IsPlan
                && !task.IsCompleted
                && task.ScheduledAt.HasValue
                && task.ScheduledAt.Value.Date == tomorrowDate)
            .Select(task => new
            {
                task.Id,
                task.UserId,
                ScheduledAt = task.ScheduledAt!.Value
            })
            .ToListAsync(cancellationToken);

        var currentReminders = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var plan in plansDueTomorrow)
        {
            currentReminders[BuildPlanReminderKey(plan.Id, plan.ScheduledAt)] = plan.UserId;
        }

        foreach (var (key, userId) in currentReminders)
        {
            if (!_knownPlanReminderRecipients.TryGetValue(key, out var knownUserId) || knownUserId != userId)
            {
                affectedUsers.Add(userId);
            }
        }

        foreach (var (key, userId) in _knownPlanReminderRecipients)
        {
            if (!currentReminders.ContainsKey(key))
            {
                affectedUsers.Add(userId);
            }
        }

        _knownPlanReminderRecipients.Clear();
        foreach (var (key, userId) in currentReminders)
        {
            _knownPlanReminderRecipients[key] = userId;
        }

        return affectedUsers;
    }

    private async Task<HashSet<int>> DetectStreakWarningChangesAsync(AppDbContext context, DateOnly today, CancellationToken cancellationToken)
    {
        var affectedUsers = new HashSet<int>();
        var usersAtRisk = await context.Users.AsNoTracking()
            .Where(user => user.CurrentStreak > 0
                && user.LastCompletionDate.HasValue
                && today.DayNumber - user.LastCompletionDate.Value.DayNumber > 0)
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);

        var currentWarnings = new Dictionary<int, DateOnly>();
        foreach (var userId in usersAtRisk)
        {
            currentWarnings[userId] = today;
        }

        foreach (var (userId, warningDate) in currentWarnings)
        {
            if (!_knownStreakWarnings.TryGetValue(userId, out var lastWarningDate) || lastWarningDate != warningDate)
            {
                affectedUsers.Add(userId);
            }
        }

        foreach (var (userId, _) in _knownStreakWarnings)
        {
            if (!currentWarnings.ContainsKey(userId))
            {
                affectedUsers.Add(userId);
            }
        }

        _knownStreakWarnings.Clear();
        foreach (var (userId, warningDate) in currentWarnings)
        {
            _knownStreakWarnings[userId] = warningDate;
        }

        return affectedUsers;
    }

    private async Task PushDashboardNotificationsAsync(
        INotificationService notificationService,
        IHubContext<NotificationsHub> hubContext,
        int userId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        var result = await notificationService.GetDashboardNotificationsAsync(userId, utcNow);
        if (!result.Success || result.Data == null)
        {
            return;
        }

        await hubContext.Clients.User(userId.ToString(CultureInfo.InvariantCulture))
            .SendAsync("notifications-updated", result.Data, cancellationToken);
    }

    private static string BuildFriendRequestKey(int requestId)
        => requestId.ToString(CultureInfo.InvariantCulture);

    private static string BuildPlanReminderKey(int planId, DateTime scheduledAt)
        => FormattableString.Invariant($"{planId}:{scheduledAt:yyyyMMdd}");
}





