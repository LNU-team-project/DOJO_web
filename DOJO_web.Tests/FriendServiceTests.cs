using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Data;
using DOJO2.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace DOJO_web.Tests;

public class FriendServiceTests
{
    private static Mock<DbSet<T>> CreateDbSetMock<T>(List<T> list) where T : class
    {
        var queryable = list.AsQueryable();
        var mockSet = new Mock<DbSet<T>>();

        mockSet.As<IAsyncEnumerable<T>>()
            .Setup(d => d.GetAsyncEnumerator(It.IsAny<CancellationToken>()))
            .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

        mockSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(new TestAsyncQueryProvider<T>(queryable.Provider));
        mockSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
        mockSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
        mockSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(() => queryable.GetEnumerator());

        mockSet.Setup(d => d.AddAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .Callback<T, CancellationToken>((s, ct) => list.Add(s))
            .Returns(ValueTask.FromResult<EntityEntry<T>>(null!));

        mockSet.Setup(d => d.Remove(It.IsAny<T>())).Callback<T>(s => list.Remove(s));
        mockSet.Setup(d => d.RemoveRange(It.IsAny<IEnumerable<T>>())).Callback<IEnumerable<T>>(items =>
        {
            foreach (var it in items.ToList()) list.Remove(it);
        });

        return mockSet;
    }

    private static UserService BuildService(Mock<UserManager<AppUser>> userManagerMock, Mock<IAppDbContext> contextMock)
    {
        var logger = new Mock<ILogger<UserService>>();
        var env = new Mock<IWebHostEnvironment>();
        return new UserService(userManagerMock.Object, logger.Object, env.Object, contextMock.Object);
    }

    private static Mock<UserManager<AppUser>> BuildUserManagerMock(IEnumerable<AppUser> users)
    {
        var store = new Mock<IUserStore<AppUser>>();
        var options = new OptionsWrapper<IdentityOptions>(new IdentityOptions());
        var passwordHasher = new PasswordHasher<AppUser>();
        var userValidators = new[] { new UserValidator<AppUser>() };
        var passwordValidators = new[] { new PasswordValidator<AppUser>() };
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var services = new ServiceCollection().BuildServiceProvider();
        var logger = new Logger<UserManager<AppUser>>(new LoggerFactory());

        var mgr = new Mock<UserManager<AppUser>>(store.Object, options, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger);
        var queryableUsers = new TestAsyncEnumerable<AppUser>(users);
        mgr.Setup(u => u.Users).Returns(queryableUsers);
        return mgr;
    }

    private static AppUser BuildUser(int id, string userName, string email)
    {
        return new AppUser
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant()
        };
    }

    [Fact]
    public async Task AddFriendAsync_ReturnsFailure_WhenAddingSelf()
    {
        var users = new List<AppUser>();
        var userManager = BuildUserManagerMock(users);
        var friends = new List<Friend>();
        var friendSet = CreateDbSetMock(friends);
        var frRequests = new List<FriendRequest>();
        var frSet = CreateDbSetMock(frRequests);

        var contextMock = new Mock<IAppDbContext>();
        contextMock.Setup(c => c.Friends).Returns(friendSet.Object);
        contextMock.Setup(c => c.FriendRequests).Returns(frSet.Object);
        contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var service = BuildService(userManager, contextMock);

        var result = await service.AddFriendAsync(1, 1);

        Assert.False(result.Success);
        Assert.Contains("себе", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddFriendAsync_SavesFriend_WhenUserExists()
    {
        var users = new List<AppUser> { BuildUser(2, "friend", "friend@example.com") };
        var userManager = BuildUserManagerMock(users);

        var friends = new List<Friend>();
        var friendSet = CreateDbSetMock(friends);
        var frRequests = new List<FriendRequest>();
        var frSet = CreateDbSetMock(frRequests);

        var contextMock = new Mock<IAppDbContext>();
        contextMock.Setup(c => c.Friends).Returns(friendSet.Object);
        contextMock.Setup(c => c.FriendRequests).Returns(frSet.Object);
        contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var service = BuildService(userManager, contextMock);

        var result = await service.AddFriendAsync(1, 2);

        Assert.True(result.Success);
        Assert.Contains(friends, f => f.UserId == 1 && f.FriendUserId == 2);
    }

    [Fact]
    public async Task AddFriendAsync_ReturnsFailure_WhenDuplicate()
    {
        var users = new List<AppUser> { BuildUser(3, "friend", "friend3@example.com") };
        var userManager = BuildUserManagerMock(users);

        var friends = new List<Friend> { new Friend { UserId = 1, FriendUserId = 3 } };
        var friendSet = CreateDbSetMock(friends);
        var frRequests = new List<FriendRequest>();
        var frSet = CreateDbSetMock(frRequests);

        var contextMock = new Mock<IAppDbContext>();
        contextMock.Setup(c => c.Friends).Returns(friendSet.Object);
        contextMock.Setup(c => c.FriendRequests).Returns(frSet.Object);
        contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var service = BuildService(userManager, contextMock);

        var result = await service.AddFriendAsync(1, 3);

        Assert.False(result.Success);
        Assert.Contains("в списку", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddFriendByUserNameAsync_ReturnsFailure_WhenUserMissing()
    {
        var users = new List<AppUser>();
        var userManager = BuildUserManagerMock(users);

        var friends = new List<Friend>();
        var friendSet = CreateDbSetMock(friends);
        var frRequests = new List<FriendRequest>();
        var frSet = CreateDbSetMock(frRequests);

        var contextMock = new Mock<IAppDbContext>();
        contextMock.Setup(c => c.Friends).Returns(friendSet.Object);
        contextMock.Setup(c => c.FriendRequests).Returns(frSet.Object);
        contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var service = BuildService(userManager, contextMock);

        var result = await service.AddFriendByUserNameAsync(1, "ghost");

        Assert.False(result.Success);
        Assert.Contains("не знайдено", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddFriendByUserNameAsync_Succeeds_WhenUserExists()
    {
        var users = new List<AppUser> { BuildUser(5, "Friend", "friend5@example.com") };
        var userManager = BuildUserManagerMock(users);

        var friends = new List<Friend>();
        var friendSet = CreateDbSetMock(friends);
        var frRequests = new List<FriendRequest>();
        var frSet = CreateDbSetMock(frRequests);

        var contextMock = new Mock<IAppDbContext>();
        contextMock.Setup(c => c.Friends).Returns(friendSet.Object);
        contextMock.Setup(c => c.FriendRequests).Returns(frSet.Object);
        contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var service = BuildService(userManager, contextMock);

        var result = await service.AddFriendByUserNameAsync(1, "Friend");

        Assert.True(result.Success);
        Assert.Contains(frRequests, fr => fr.RequesterUserId == 1 && fr.ReceiverUserId == 5);
        Assert.DoesNotContain(friends, f => f.UserId == 1 && f.FriendUserId == 5);
    }

    [Fact]
    public async Task AcceptFriendRequestAsync_CreatesMutualFriendship()
    {
        var users = new List<AppUser> { BuildUser(5, "Friend", "friend5@example.com"), BuildUser(1, "Owner", "owner@example.com") };
        var userManager = BuildUserManagerMock(users);

        var frRequests = new List<FriendRequest>
        {
            new FriendRequest { Id = 10, RequesterUserId = 5, ReceiverUserId = 1, CreatedAt = DateTime.UtcNow }
        };
        var frSet = CreateDbSetMock(frRequests);

        var friends = new List<Friend>();
        var friendSet = CreateDbSetMock(friends);

        var contextMock = new Mock<IAppDbContext>();
        contextMock.Setup(c => c.Friends).Returns(friendSet.Object);
        contextMock.Setup(c => c.FriendRequests).Returns(frSet.Object);
        contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var service = BuildService(userManager, contextMock);

        var result = await service.AcceptFriendRequestAsync(1, 10);

        Assert.True(result.Success);
        Assert.Contains(friends, f => f.UserId == 1 && f.FriendUserId == 5);
        Assert.Contains(friends, f => f.UserId == 5 && f.FriendUserId == 1);
        Assert.DoesNotContain(frRequests, fr => fr.Id == 10);
    }

    [Fact]
    public async Task RemoveFriendAsync_RemovesExistingEntry()
    {
        var users = new List<AppUser>();
        var userManager = BuildUserManagerMock(users);

        var friends = new List<Friend> { new Friend { UserId = 1, FriendUserId = 9 } };
        var friendSet = CreateDbSetMock(friends);
        var frRequests = new List<FriendRequest>();
        var frSet = CreateDbSetMock(frRequests);

        var contextMock = new Mock<IAppDbContext>();
        contextMock.Setup(c => c.Friends).Returns(friendSet.Object);
        contextMock.Setup(c => c.FriendRequests).Returns(frSet.Object);
        contextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var service = BuildService(userManager, contextMock);

        var result = await service.RemoveFriendAsync(1, 9);

        Assert.True(result.Success);
        Assert.DoesNotContain(friends, f => f.UserId == 1 && f.FriendUserId == 9);
    }

    // Async query helpers for mocking EF Core
    internal class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
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

        public object? Execute(Expression expression) => _inner.Execute(expression);

        public TResult Execute<TResult>(Expression expression) => _inner.Execute<TResult>(expression);

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(Task<>))
            {
                var resultType = typeof(TResult).GetGenericArguments()[0];
                var executeResult = _inner.Execute(expression);
                var fromResult = typeof(Task).GetMethod(nameof(Task.FromResult))!.MakeGenericMethod(resultType);
                return (TResult)fromResult.Invoke(null, new[] { executeResult })!;
            }

            return Execute<TResult>(expression);
        }
    }

    internal class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }
        public TestAsyncEnumerable(Expression expression) : base(expression) { }
        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;
        public TestAsyncEnumerator(IEnumerator<T> inner) { _inner = inner; }
        public T Current => _inner.Current;
        public ValueTask DisposeAsync() { _inner.Dispose(); return ValueTask.CompletedTask; }
        public ValueTask<bool> MoveNextAsync() => new ValueTask<bool>(_inner.MoveNext());
    }
}
