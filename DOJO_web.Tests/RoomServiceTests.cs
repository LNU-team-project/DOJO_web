using System.Linq.Expressions;
using DOJO2.Application.ViewModels;
using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Data;
using DOJO2.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace DOJO_web.Tests;

public class RoomServiceTests
{
    private sealed class IncludeStrippingExpressionVisitor : ExpressionVisitor
    {
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(EntityFrameworkQueryableExtensions)
                && (node.Method.Name is nameof(EntityFrameworkQueryableExtensions.Include)
                    or nameof(EntityFrameworkQueryableExtensions.ThenInclude)))
            {
                return Visit(node.Arguments[0]);
            }

            return base.VisitMethodCall(node);
        }
    }

    private sealed class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        public TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
            => new TestAsyncEnumerable<TEntity>(expression);

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => new TestAsyncEnumerable<TElement>(expression);

        public object? Execute(Expression expression)
            => _inner.Execute(StripIncludes(expression));

        public TResult Execute<TResult>(Expression expression)
            => _inner.Execute<TResult>(StripIncludes(expression));

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            var stripped = StripIncludes(expression);

            if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultType = typeof(TResult).GetGenericArguments()[0];
                var executeResult = _inner.Execute(stripped);
                var fromResult = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType);
                return (TResult)fromResult.Invoke(null, new[] { executeResult })!;
            }

            return Execute<TResult>(stripped);
        }

        private static Expression StripIncludes(Expression expression)
            => new IncludeStrippingExpressionVisitor().Visit(expression);
    }

    private sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(Expression expression) : base(expression)
        {
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new TestAsyncEnumerator<T>(((IEnumerable<T>)this).GetEnumerator());

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    private sealed class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public T Current => _inner.Current;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync() => new(_inner.MoveNext());
    }

    private static Mock<DbSet<T>> BuildMockDbSet<T>(IList<T> source) where T : class
    {
        var queryable = source.AsQueryable();
        var dbSet = new Mock<DbSet<T>>();
        dbSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(queryable.Provider));
        dbSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        dbSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        dbSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());
        dbSet.As<IAsyncEnumerable<T>>().Setup(m => m.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(() => new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

        return dbSet;
    }

    private static void SetupAddAsync<T>(Mock<DbSet<T>> dbSet, Action<T> onAdd) where T : class
    {
        dbSet.Setup(d => d.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .Callback<T, CancellationToken>((entity, _) => onAdd(entity))
            .Returns(new ValueTask<EntityEntry<T>>((EntityEntry<T>)null!));
    }

    private static void SetupAdd<T>(Mock<DbSet<T>> dbSet, Action<T> onAdd) where T : class
    {
        dbSet.Setup(d => d.Add(It.IsAny<T>()))
            .Callback(onAdd)
            .Returns((EntityEntry<T>)null!);
    }

    private static void SetupRemove<T>(Mock<DbSet<T>> dbSet, Action<T> onRemove) where T : class
    {
        dbSet.Setup(d => d.Remove(It.IsAny<T>()))
            .Callback(onRemove)
            .Returns((EntityEntry<T>)null!);
    }

    private static Mock<UserManager<AppUser>> BuildUserManager(List<AppUser> users)
    {
        var userSet = BuildMockDbSet(users);
        var manager = new Mock<UserManager<AppUser>>(
            new Mock<IUserStore<AppUser>>().Object,
            Options.Create(new IdentityOptions()),
            Mock.Of<IPasswordHasher<AppUser>>(),
            Array.Empty<IUserValidator<AppUser>>(),
            Array.Empty<IPasswordValidator<AppUser>>(),
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<AppUser>>>());

        manager.Setup(m => m.Users).Returns(userSet.Object);
        return manager;
    }

    private static RoomService BuildService(
        out Mock<IAppDbContext> contextMock,
        List<AppUser>? users = null,
        List<Room>? rooms = null,
        List<RoomMember>? roomMembers = null,
        List<RoomTask>? roomTasks = null,
        List<RoomTaskComment>? comments = null,
        List<TaskItem>? taskItems = null)
    {
        users ??= new List<AppUser>();
        rooms ??= new List<Room>();
        roomMembers ??= new List<RoomMember>();
        roomTasks ??= new List<RoomTask>();
        comments ??= new List<RoomTaskComment>();
        taskItems ??= new List<TaskItem>();

        var roomsSet = BuildMockDbSet(rooms);
        var roomMembersSet = BuildMockDbSet(roomMembers);
        var roomTasksSet = BuildMockDbSet(roomTasks);
        var commentsSet = BuildMockDbSet(comments);
        var taskItemsSet = BuildMockDbSet(taskItems);

        var nextRoomId = rooms.Any() ? rooms.Max(item => item.Id) + 1 : 1;
        var nextRoomMemberId = roomMembers.Any() ? roomMembers.Max(item => item.Id) + 1 : 1;
        var nextRoomTaskId = roomTasks.Any() ? roomTasks.Max(item => item.Id) + 1 : 1;
        var nextCommentId = comments.Any() ? comments.Max(item => item.Id) + 1 : 1;
        var nextTaskItemId = taskItems.Any() ? taskItems.Max(item => item.Id) + 1 : 1;

        SetupAddAsync(roomsSet, room =>
        {
            if (room.Id <= 0)
            {
                room.Id = nextRoomId++;
            }

            rooms.Add(room);
        });

        SetupAddAsync(roomMembersSet, member =>
        {
            if (member.Id <= 0)
            {
                member.Id = nextRoomMemberId++;
            }

            roomMembers.Add(member);
        });

        SetupRemove(roomMembersSet, member => roomMembers.Remove(member));

        SetupAddAsync(roomTasksSet, task =>
        {
            if (task.Id <= 0)
            {
                task.Id = nextRoomTaskId++;
            }

            roomTasks.Add(task);
        });

        SetupAddAsync(commentsSet, comment =>
        {
            if (comment.Id <= 0)
            {
                comment.Id = nextCommentId++;
            }

            comments.Add(comment);
        });

        SetupAdd(taskItemsSet, taskItem =>
        {
            if (taskItem.Id <= 0)
            {
                taskItem.Id = nextTaskItemId++;
            }

            taskItems.Add(taskItem);
        });

        contextMock = new Mock<IAppDbContext>(MockBehavior.Strict);
        contextMock.Setup(c => c.Rooms).Returns(roomsSet.Object);
        contextMock.Setup(c => c.RoomMembers).Returns(roomMembersSet.Object);
        contextMock.Setup(c => c.RoomTasks).Returns(roomTasksSet.Object);
        contextMock.Setup(c => c.RoomTaskComments).Returns(commentsSet.Object);
        contextMock.Setup(c => c.Tasks).Returns(taskItemsSet.Object);
        contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var userManagerMock = BuildUserManager(users);
        var loggerMock = new Mock<ILogger<RoomService>>();
        return new RoomService(contextMock.Object, userManagerMock.Object, loggerMock.Object);
    }

    private static AppUser BuildUser(int id, string userName)
        => new()
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            AvatarUrl = $"https://example.com/{userName}.png"
        };

    private static RoomMember BuildMember(int id, int roomId, int userId, AppUser? user = null, DateTime? joinedAt = null)
        => new()
        {
            Id = id,
            RoomId = roomId,
            UserId = userId,
            User = user,
            JoinedAt = joinedAt ?? new DateTime(2026, 4, 1, 12, 0, 0, DateTimeKind.Utc)
        };

    private static RoomTaskComment BuildComment(int id, int taskId, int authorUserId, AppUser? authorUser = null, string text = "Comment")
        => new()
        {
            Id = id,
            TaskId = taskId,
            AuthorUserId = authorUserId,
            AuthorUser = authorUser,
            Text = text,
            CreatedAt = new DateTime(2026, 4, 1, 13, 0, 0, DateTimeKind.Utc)
        };

    private static RoomTask BuildRoomTask(
        int id,
        int roomId,
        int assignedToUserId,
        AppUser? assignedToUser = null,
        string title = "Task",
        string? description = null,
        List<RoomTaskComment>? comments = null,
        Room? room = null)
        => new()
        {
            Id = id,
            RoomId = roomId,
            AssignedToUserId = assignedToUserId,
            AssignedToUser = assignedToUser,
            Title = title,
            Description = description,
            IsCompleted = false,
            DueDate = new DateTime(2026, 4, 5, 18, 0, 0, DateTimeKind.Utc),
            CreatedAt = new DateTime(2026, 4, 1, 14, 0, 0, DateTimeKind.Utc),
            Comments = comments ?? new List<RoomTaskComment>(),
            Room = room
        };

    private static Room BuildRoom(
        int id,
        int ownerUserId,
        string title,
        string? description = null,
        List<RoomMember>? members = null,
        List<RoomTask>? tasks = null)
        => new()
        {
            Id = id,
            OwnerUserId = ownerUserId,
            Title = title,
            Description = description,
            CreatedAt = new DateTime(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc),
            Members = members ?? new List<RoomMember>(),
            Tasks = tasks ?? new List<RoomTask>()
        };

    [Fact]
    public async Task GetMyRoomsAsync_ReturnsFailure_WhenUserIdIsInvalid()
    {
        var service = BuildService(out _);

        var result = await service.GetMyRoomsAsync(0);

        Assert.False(result.Success);
        Assert.Equal("Невалідний ідентифікатор користувача", result.Message);
    }

    [Fact]
    public async Task GetMyRoomsAsync_ReturnsOwnedAndJoinedRooms()
    {
        var owner = BuildUser(1, "owner");
        var member = BuildUser(2, "member");
        var other = BuildUser(3, "other");

        var rooms = new List<Room>
        {
            BuildRoom(10, 1, "Owner room", "Owned by user", new List<RoomMember>
            {
                BuildMember(101, 10, 1, owner),
                BuildMember(102, 10, 2, member)
            }, new List<RoomTask>
            {
                BuildRoomTask(201, 10, 2, member, "Prep", "Prepare materials", new List<RoomTaskComment>
                {
                    BuildComment(301, 201, 1, owner, "Looks good")
                })
            }),
            BuildRoom(11, 3, "Member room", null, new List<RoomMember>
            {
                BuildMember(103, 11, 2, member)
            }),
            BuildRoom(12, 3, "Unrelated room", null, new List<RoomMember>
            {
                BuildMember(104, 12, 3, other)
            })
        };

        var service = BuildService(out _, new List<AppUser> { owner, member, other }, rooms);

        var result = await service.GetMyRoomsAsync(2);

        Assert.True(result.Success);
        Assert.Equal("Кімнати завантажено", result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data!.Count);
        var first = result.Data[0];
        Assert.Equal(10, first.Id);
        Assert.Equal("Owner room", first.Title);
        Assert.Equal(2, first.Members.Count);
        Assert.Single(first.Tasks);
        Assert.Equal("owner", first.Members[0].UserName);
        Assert.Equal("member", first.Members[1].UserName);
        Assert.Equal("owner", first.Tasks[0].Comments[0].AuthorUserName);

        var second = result.Data[1];
        Assert.Equal(11, second.Id);
        Assert.Equal("Member room", second.Title);
        Assert.Single(second.Members);
        Assert.Empty(second.Tasks);
    }

    [Fact]
    public async Task GetRoomAsync_ReturnsFailure_WhenUserHasNoAccess()
    {
        var room = BuildRoom(5, 1, "Private room", null, new List<RoomMember>
        {
            BuildMember(1, 5, 2, BuildUser(2, "member"))
        });

        var service = BuildService(out _, rooms: new List<Room> { room });

        var result = await service.GetRoomAsync(3, 5);

        Assert.False(result.Success);
        Assert.Equal("Доступ заборонено", result.Message);
    }

    [Fact]
    public async Task GetRoomAsync_ReturnsMappedRoom_WhenUserHasAccess()
    {
        var owner = BuildUser(1, "owner");
        var member = BuildUser(2, "member");
        var commenter = BuildUser(3, "commenter");

        var room = BuildRoom(15, 1, "Study room", "Focus here", new List<RoomMember>
        {
            BuildMember(11, 15, 1, owner),
            BuildMember(12, 15, 2, member)
        }, new List<RoomTask>
        {
            BuildRoomTask(21, 15, 2, member, "Task A", "Details", new List<RoomTaskComment>
            {
                BuildComment(31, 21, 3, commenter, "First comment")
            })
        });

        var service = BuildService(out _, new List<AppUser> { owner, member, commenter }, new List<Room> { room });

        var result = await service.GetRoomAsync(2, 15);

        Assert.True(result.Success);
        Assert.Equal("Кімнату завантажено", result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal(15, result.Data!.Id);
        Assert.Equal("Study room", result.Data.Title);
        Assert.Equal("Focus here", result.Data.Description);
        Assert.Equal(2, result.Data.Members.Count);
        Assert.Single(result.Data.Tasks);
        Assert.Equal("owner", result.Data.Members[0].UserName);
        Assert.Equal("member", result.Data.Members[1].UserName);
        Assert.Equal("member", result.Data.Tasks[0].AssignedToUserName);
        Assert.Equal("commenter", result.Data.Tasks[0].Comments[0].AuthorUserName);
    }

    [Fact]
    public async Task CreateRoomAsync_ReturnsFailure_WhenInputIsInvalid()
    {
        var service = BuildService(out _);

        var result = await service.CreateRoomAsync(1, new CreateRoomRequest { Title = "   " });

        Assert.False(result.Success);
        Assert.Equal("Невірні дані для створення кімнати", result.Message);
    }

    [Fact]
    public async Task CreateRoomAsync_CreatesRoomAndUniqueMembers()
    {
        var users = new List<AppUser>
        {
            BuildUser(2, "alice"),
            BuildUser(3, "bob")
        };

        var request = new CreateRoomRequest
        {
            Title = "  Planning room  ",
            Description = "  Team sync  ",
            MemberUserIds = new List<int> { 2, 2, 1, 3, 0, -1, 4 }
        };

        var service = BuildService(out var contextMock, users);

        var result = await service.CreateRoomAsync(1, request);

        Assert.True(result.Success);
        Assert.Equal("Кімнату створено", result.Message);
        Assert.Equal(1, result.Data);
        Assert.Single(contextMock.Object.Rooms.Where(r => r.Id == result.Data));
        Assert.Equal("Planning room", contextMock.Object.Rooms.Single(r => r.Id == result.Data).Title);
        Assert.Equal("Team sync", contextMock.Object.Rooms.Single(r => r.Id == result.Data).Description);

        Assert.Equal(3, contextMock.Object.RoomMembers.Count());
        Assert.Contains(contextMock.Object.RoomMembers, m => m.UserId == 1);
        Assert.Contains(contextMock.Object.RoomMembers, m => m.UserId == 2);
        Assert.Contains(contextMock.Object.RoomMembers, m => m.UserId == 3);
        Assert.Equal(2, contextMock.Object.RoomMembers.Count(m => m.UserId != 1));
        contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task AddMemberAsync_ReturnsFailure_WhenUserIsNotOwner()
    {
        var room = BuildRoom(7, 1, "Room", members: new List<RoomMember>
        {
            BuildMember(1, 7, 1, BuildUser(1, "owner"))
        });

        var service = BuildService(out _, users: new List<AppUser> { BuildUser(5, "member") }, rooms: new List<Room> { room });

        var result = await service.AddMemberAsync(2, 7, 5);

        Assert.False(result.Success);
        Assert.Equal("Тільки власник може додавати учасників", result.Message);
    }

    [Fact]
    public async Task AddMemberAsync_AddsMember_WhenRequestIsValid()
    {
        var owner = BuildUser(1, "owner");
        var invited = BuildUser(5, "invited");
        var room = BuildRoom(9, 1, "Room", members: new List<RoomMember>
        {
            BuildMember(1, 9, 1, owner)
        });

        var service = BuildService(out var contextMock, users: new List<AppUser> { owner, invited }, rooms: new List<Room> { room }, roomMembers: room.Members);

        var result = await service.AddMemberAsync(1, 9, 5);

        Assert.True(result.Success);
        Assert.True(result.Data);
        Assert.Contains(contextMock.Object.RoomMembers, m => m.UserId == 5 && m.RoomId == 9);
        contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveMemberAsync_ReturnsFailure_WhenUserHasNoRights()
    {
        var room = BuildRoom(20, 1, "Room", members: new List<RoomMember>
        {
            BuildMember(1, 20, 1, BuildUser(1, "owner")),
            BuildMember(2, 20, 4, BuildUser(4, "member"))
        });

        var service = BuildService(out _, rooms: new List<Room> { room }, roomMembers: room.Members);

        var result = await service.RemoveMemberAsync(2, 20, 4);

        Assert.False(result.Success);
        Assert.Equal("Немає прав для видалення учасника", result.Message);
    }

    [Fact]
    public async Task RemoveMemberAsync_RemovesMember_WhenUserIsOwner()
    {
        var owner = BuildUser(1, "owner");
        var invited = BuildUser(4, "member");
        var roomMembers = new List<RoomMember>
        {
            BuildMember(1, 21, 1, owner),
            BuildMember(2, 21, 4, invited)
        };
        var room = BuildRoom(21, 1, "Room", members: roomMembers);

        var service = BuildService(out var contextMock, users: new List<AppUser> { owner, invited }, rooms: new List<Room> { room }, roomMembers: roomMembers);

        var result = await service.RemoveMemberAsync(1, 21, 4);

        Assert.True(result.Success);
        Assert.Empty(contextMock.Object.RoomMembers.Where(m => m.UserId == 4 && m.RoomId == 21));
        contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateTaskAsync_ReturnsFailure_WhenAssignedUserIsNotInRoom()
    {
        var owner = BuildUser(1, "owner");
        var member = BuildUser(2, "member");
        var outsider = BuildUser(3, "outsider");
        var room = BuildRoom(30, 1, "Team room", members: new List<RoomMember>
        {
            BuildMember(1, 30, 1, owner),
            BuildMember(2, 30, 2, member)
        });

        var service = BuildService(out _, new List<AppUser> { owner, member, outsider }, new List<Room> { room }, roomMembers: room.Members);

        var result = await service.CreateTaskAsync(1, 30, new CreateRoomTaskRequest
        {
            Title = "Prepare notes",
            AssignedToUserId = 3
        });

        Assert.False(result.Success);
        Assert.Equal("Користувача для призначення не знайдено в учасниках кімнати", result.Message);
    }

    [Fact]
    public async Task CreateTaskAsync_CreatesRoomTaskAndTodoTask()
    {
        var owner = BuildUser(1, "owner");
        var assignee = BuildUser(2, "member");
        var room = BuildRoom(31, 1, "Weekly room", members: new List<RoomMember>
        {
            BuildMember(1, 31, 1, owner),
            BuildMember(2, 31, 2, assignee)
        });

        var roomTasks = new List<RoomTask>();
        var taskItems = new List<TaskItem>();
        var service = BuildService(out var contextMock, new List<AppUser> { owner, assignee }, new List<Room> { room }, roomMembers: room.Members, roomTasks: roomTasks, taskItems: taskItems);

        var dueDate = new DateTime(2026, 4, 30, 17, 45, 0, DateTimeKind.Utc);
        var result = await service.CreateTaskAsync(1, 31, new CreateRoomTaskRequest
        {
            Title = "  Write summary  ",
            Description = "  Team updates  ",
            AssignedToUserId = 2,
            DueDate = dueDate
        });

        Assert.True(result.Success);
        Assert.Equal("Завдання створено", result.Message);
        Assert.Single(roomTasks);
        Assert.Single(taskItems);

        var roomTask = roomTasks[0];
        Assert.Equal(31, roomTask.RoomId);
        Assert.Equal(2, roomTask.AssignedToUserId);
        Assert.Equal("Write summary", roomTask.Title);
        Assert.Equal("Team updates", roomTask.Description);
        Assert.Equal(dueDate, roomTask.DueDate);

        var todo = taskItems[0];
        Assert.Equal(2, todo.UserId);
        Assert.Equal("[Weekly room] Write summary", todo.Title);
        Assert.Equal("Team updates", todo.Description);
        Assert.False(todo.IsCompleted);
        Assert.Equal(DateOnly.FromDateTime(dueDate), todo.DueDate);
        Assert.Equal(2, todo.Priority);
        contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }


    [Fact]
    public async Task AddCommentAsync_ReturnsFailure_WhenUserHasNoAccess()
    {
        var owner = BuildUser(1, "owner");
        var member = BuildUser(2, "member");
        var outsider = BuildUser(3, "outsider");
        var room = BuildRoom(60, 1, "Room", members: new List<RoomMember>
        {
            BuildMember(1, 60, 1, owner),
            BuildMember(2, 60, 2, member)
        });
        var task = BuildRoomTask(61, 60, 2, member, room: room);

        var service = BuildService(out _, new List<AppUser> { owner, member, outsider }, new List<Room> { room }, roomMembers: room.Members, roomTasks: new List<RoomTask> { task });

        var result = await service.AddCommentAsync(3, 61, "Hello");

        Assert.False(result.Success);
        Assert.Equal("Доступ заборонено", result.Message);
    }

    [Fact]
    public async Task AddCommentAsync_AddsComment_WhenUserIsRoomMember()
    {
        var owner = BuildUser(1, "owner");
        var member = BuildUser(2, "member");
        var room = BuildRoom(70, 1, "Room", members: new List<RoomMember>
        {
            BuildMember(1, 70, 1, owner),
            BuildMember(2, 70, 2, member)
        });
        var task = BuildRoomTask(71, 70, 2, member, room: room);
        var comments = new List<RoomTaskComment>();

        var service = BuildService(out var contextMock, new List<AppUser> { owner, member }, new List<Room> { room }, roomMembers: room.Members, roomTasks: new List<RoomTask> { task }, comments: comments);

        var result = await service.AddCommentAsync(2, 71, "  Great work!  ");

        Assert.True(result.Success);
        Assert.Equal("Коментар додано", result.Message);
        Assert.Single(comments);
        Assert.Equal("Great work!", comments[0].Text);
        Assert.Equal(71, comments[0].TaskId);
        Assert.Equal(2, comments[0].AuthorUserId);
        contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}