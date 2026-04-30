using System.Linq;
using DOJO2.Application.Common;
using DOJO2.Application.Interfaces;
using DOJO2.Application.ViewModels;
using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DOJO2.Infrastructure.Services;

public class RoomService : IRoomService
{
    private readonly IAppDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<RoomService> _logger;

    public RoomService(IAppDbContext context, UserManager<AppUser> userManager, ILogger<RoomService> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Result<List<RoomViewModel>>> GetMyRoomsAsync(int userId)
    {
        if (userId <= 0) return Result<List<RoomViewModel>>.FailureResult("Невалідний ідентифікатор користувача");

        var rooms = await _context.Rooms
            .Where(r => r.OwnerUserId == userId || r.Members.Any(m => m.UserId == userId))
            .Include(r => r.Members).ThenInclude(m => m.User)
            .Include(r => r.Tasks).ThenInclude(t => t.Comments)
            .ToListAsync();

        var result = rooms.Select(r => MapToViewModel(r)).ToList();
        return Result<List<RoomViewModel>>.SuccessResult(result, "Кімнати завантажено");
    }

    public async Task<Result<RoomViewModel>> GetRoomAsync(int userId, int roomId)
    {
        if (userId <= 0 || roomId <= 0) return Result<RoomViewModel>.FailureResult("Невалідні дані");

        var room = await _context.Rooms
            .Include(r => r.Members).ThenInclude(m => m.User)
            .Include(r => r.Tasks).ThenInclude(t => t.AssignedToUser)
            .Include(r => r.Tasks).ThenInclude(t => t.Comments).ThenInclude(c => c.AuthorUser)
            .FirstOrDefaultAsync(r => r.Id == roomId);

        if (room == null) return Result<RoomViewModel>.FailureResult("Кімнату не знайдено");

        var isMember = room.OwnerUserId == userId || room.Members.Any(m => m.UserId == userId);
        if (!isMember) return Result<RoomViewModel>.FailureResult("Доступ заборонено");

        return Result<RoomViewModel>.SuccessResult(MapToViewModel(room), "Кімнату завантажено");
    }

    public async Task<Result<int>> CreateRoomAsync(int userId, CreateRoomRequest model)
    {
        if (userId <= 0) return Result<int>.FailureResult("Невалідний ідентифікатор користувача");
        if (model == null || string.IsNullOrWhiteSpace(model.Title)) return Result<int>.FailureResult("Невірні дані для створення кімнати");

        var now = DateTime.UtcNow;
        var room = new Room
        {
            OwnerUserId = userId,
            Title = model.Title.Trim(),
            Description = model.Description?.Trim(),
            CreatedAt = now
        };

        await _context.Rooms.AddAsync(room);
        await _context.SaveChangesAsync();

        // Add owner as member
        var ownerMember = new RoomMember { RoomId = room.Id, UserId = userId, JoinedAt = now };
        await _context.RoomMembers.AddAsync(ownerMember);

        // Add other members if provided and exist
        var uniqueIds = (model.MemberUserIds ?? new List<int>()).Where(id => id > 0).Distinct().ToList();
        foreach (var memberId in uniqueIds)
        {
            if (memberId == userId) continue;
            var exists = await _userManager.Users.AnyAsync(u => u.Id == memberId);
            if (!exists) continue;
            var member = new RoomMember { RoomId = room.Id, UserId = memberId, JoinedAt = now };
            await _context.RoomMembers.AddAsync(member);
        }

        await _context.SaveChangesAsync();

        return Result<int>.SuccessResult(room.Id, "Кімнату створено");
    }

    public async Task<Result<bool>> AddMemberAsync(int userId, int roomId, int memberUserId)
    {
        if (userId <= 0 || roomId <= 0 || memberUserId <= 0) return Result<bool>.FailureResult("Невалідні дані");

        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId);
        if (room == null) return Result<bool>.FailureResult("Кімнату не знайдено");
        if (room.OwnerUserId != userId) return Result<bool>.FailureResult("Тільки власник може додавати учасників");

        var userExists = await _userManager.Users.AnyAsync(u => u.Id == memberUserId);
        if (!userExists) return Result<bool>.FailureResult("Користувача не знайдено");

        var already = await _context.RoomMembers.AnyAsync(m => m.RoomId == roomId && m.UserId == memberUserId);
        if (already) return Result<bool>.FailureResult("Користувач вже є учасником");

        await _context.RoomMembers.AddAsync(new RoomMember { RoomId = roomId, UserId = memberUserId, JoinedAt = DateTime.UtcNow });
        await _context.SaveChangesAsync();

        return Result<bool>.SuccessResult(true, "Учасника додано");
    }

    public async Task<Result<bool>> RemoveMemberAsync(int userId, int roomId, int memberUserId)
    {
        if (userId <= 0 || roomId <= 0 || memberUserId <= 0) return Result<bool>.FailureResult("Невалідні дані");

        var room = await _context.Rooms.FirstOrDefaultAsync(r => r.Id == roomId);
        if (room == null) return Result<bool>.FailureResult("Кімнату не знайдено");

        var isOwner = room.OwnerUserId == userId;
        var isSelf = userId == memberUserId;
        if (!isOwner && !isSelf) return Result<bool>.FailureResult("Немає прав для видалення учасника");

        var entry = await _context.RoomMembers.FirstOrDefaultAsync(m => m.RoomId == roomId && m.UserId == memberUserId);
        if (entry == null) return Result<bool>.FailureResult("Учасника не знайдено");

        _context.RoomMembers.Remove(entry);
        await _context.SaveChangesAsync();

        return Result<bool>.SuccessResult(true, "Учасника видалено");
    }

    public async Task<Result<int>> CreateTaskAsync(int userId, int roomId, CreateRoomTaskRequest request)
    {
        if (userId <= 0 || roomId <= 0 || request == null || string.IsNullOrWhiteSpace(request.Title))
            return Result<int>.FailureResult("Невірні дані");

        var room = await _context.Rooms.Include(r => r.Members).FirstOrDefaultAsync(r => r.Id == roomId);
        if (room == null) return Result<int>.FailureResult("Кімнату не знайдено");

        var isMember = room.OwnerUserId == userId || room.Members.Any(m => m.UserId == userId);
        if (!isMember) return Result<int>.FailureResult("Доступ заборонено");

        var assignedIsMember = room.OwnerUserId == request.AssignedToUserId || room.Members.Any(m => m.UserId == request.AssignedToUserId);
        if (!assignedIsMember) return Result<int>.FailureResult("Користувача для призначення не знайдено в учасниках кімнати");

        var task = new RoomTask
        {
            RoomId = roomId,
            AssignedToUserId = request.AssignedToUserId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            IsCompleted = false,
            DueDate = request.DueDate,
            CreatedAt = DateTime.UtcNow
        };

        await _context.RoomTasks.AddAsync(task);
        await _context.SaveChangesAsync();

        // Також створюємо завдання в загальному списку для призначеної людини
        try
        {
            DateOnly? dueDateOnly = null;
            if (request.DueDate.HasValue)
            {
                dueDateOnly = new DateOnly(request.DueDate.Value.Year, request.DueDate.Value.Month, request.DueDate.Value.Day);
            }

            var todoForUser = new TaskItem
            {
                UserId = request.AssignedToUserId,
                Title = $"[{room.Title}] {request.Title.Trim()}",
                Description = request.Description?.Trim(),
                Priority = 2,
                DueDate = dueDateOnly,
                IsCompleted = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Tasks.Add(todoForUser);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Завдання кімнати {RoomTaskId} та TODO {TodoId} створено", task.Id, todoForUser.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Помилка при створенні TODO для користувача: {Error}", ex.Message);
        }

        return Result<int>.SuccessResult(task.Id, "Завдання створено");
    }

    public async Task<Result<bool>> AddCommentAsync(int userId, int taskId, string text)
    {
        if (userId <= 0 || taskId <= 0 || string.IsNullOrWhiteSpace(text)) return Result<bool>.FailureResult("Невірні дані");

        var task = await _context.RoomTasks.Include(t => t.Room).FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null) return Result<bool>.FailureResult("Завдання не знайдено");

        var room = task.Room;
        if (room == null) return Result<bool>.FailureResult("Кімнату не знайдено");

        var isMember = room.OwnerUserId == userId || _context.RoomMembers.Any(m => m.RoomId == room.Id && m.UserId == userId);
        if (!isMember) return Result<bool>.FailureResult("Доступ заборонено");

        var comment = new RoomTaskComment
        {
            TaskId = taskId,
            AuthorUserId = userId,
            Text = text.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        await _context.RoomTaskComments.AddAsync(comment);
        await _context.SaveChangesAsync();

        return Result<bool>.SuccessResult(true, "Коментар додано");
    }

    private RoomViewModel MapToViewModel(Room r)
    {
        var vm = new RoomViewModel
        {
            Id = r.Id,
            OwnerUserId = r.OwnerUserId,
            Title = r.Title,
            Description = r.Description,
            CreatedAt = r.CreatedAt,
            Members = r.Members?.Select(m => new RoomMemberViewModel
            {
                Id = m.Id,
                UserId = m.UserId,
                UserName = m.User?.UserName ?? string.Empty,
                AvatarUrl = m.User?.AvatarUrl,
                JoinedAt = m.JoinedAt
            }).ToList() ?? new List<RoomMemberViewModel>(),
            Tasks = r.Tasks?.Select(t => new RoomTaskViewModel
            {
                Id = t.Id,
                RoomId = t.RoomId,
                AssignedToUserId = t.AssignedToUserId,
                AssignedToUserName = t.AssignedToUser?.UserName ?? string.Empty,
                Title = t.Title,
                Description = t.Description,
                IsCompleted = t.IsCompleted,
                DueDate = t.DueDate,
                CreatedAt = t.CreatedAt,
                Comments = t.Comments?.Select(c => new RoomTaskCommentViewModel
                {
                    Id = c.Id,
                    TaskId = c.TaskId,
                    AuthorUserId = c.AuthorUserId,
                    AuthorUserName = c.AuthorUser?.UserName ?? string.Empty,
                    Text = c.Text,
                    CreatedAt = c.CreatedAt
                }).ToList() ?? new List<RoomTaskCommentViewModel>()
            }).ToList() ?? new List<RoomTaskViewModel>()
        };

        return vm;
    }
}
