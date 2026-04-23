using System.Linq.Expressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using DOJO2.Application.Common;
using DOJO2.Application.Interfaces;
using DOJO2.Application.Services;
using DOJO2.Domain.Entities;

namespace DOJO_web.Tests;

public class LeaderboardServiceTests
{
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

    private sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(Expression expression) : base(expression) { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

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
            .Returns(new TestAsyncEnumerator<T>(queryable.GetEnumerator()));

        return dbSet;
    }

    private static LeaderboardService BuildService(List<AppUser> users)
    {
        var userSet = BuildMockDbSet(users);
        var userManagerMock = new Mock<UserManager<AppUser>>(
            new Mock<IUserStore<AppUser>>().Object,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<AppUser>>(),
            Array.Empty<IUserValidator<AppUser>>(),
            Array.Empty<IPasswordValidator<AppUser>>(),
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<AppUser>>>());
        
        userManagerMock.Setup(m => m.Users).Returns(userSet.Object);
        
        var logger = new Mock<ILogger<LeaderboardService>>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var cacheOptions = Options.Create(new CacheOptions { LeaderboardSeconds = 120 });
        return new LeaderboardService(userManagerMock.Object, logger.Object, cache, cacheOptions);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ReturnsCachedData_OnSubsequentCall()
    {
        var users = new List<AppUser>
        {
            new()
            {
                Id = 1,
                UserName = "user1",
                ExpPoints = 1000,
                Level = 5,
                Pomodoros = new List<Pomodoro>(),
                AvatarUrl = null
            }
        };

        var service = BuildService(users);

        var firstResult = await service.GetLeaderboardAsync(limit: 10);

        users[0].ExpPoints = 1500;
        var secondResult = await service.GetLeaderboardAsync(limit: 10);

        Assert.Equal(1000, firstResult.Entries[0].Score);
        Assert.Equal(1000, secondResult.Entries[0].Score);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ReturnsSuccess_WithTopUsers()
    {
        // Arrange
        var users = new List<AppUser>
        {
            new() { Id = 1, UserName = "user1", ExpPoints = 1000, Level = 5, Pomodoros = new List<Pomodoro> { new() { Id = 1 }, new() { Id = 2 } }, AvatarUrl = "avatar1.jpg" },
            new() { Id = 2, UserName = "user2", ExpPoints = 800, Level = 4, Pomodoros = new List<Pomodoro> { new() { Id = 3 } }, AvatarUrl = "avatar2.jpg" },
            new() { Id = 3, UserName = "user3", ExpPoints = 600, Level = 3, Pomodoros = new List<Pomodoro>(), AvatarUrl = null }
        };

        var service = BuildService(users);

        // Act
        var result = await service.GetLeaderboardAsync(limit: 10);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Entries);
        Assert.Equal(3, result.Entries.Count);
        Assert.Equal(1, result.Entries[0].Rank);
        Assert.Equal("user1", result.Entries[0].Username);
        Assert.Equal(1000, result.Entries[0].Score);
        Assert.Equal(5, result.Entries[0].Level);
        Assert.Equal(2, result.Entries[0].PomodoroSessions);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ReturnsSuccess_RespectLimit()
    {
        // Arrange
        var users = new List<AppUser>
        {
            new() { Id = 1, UserName = "user1", ExpPoints = 1000, Level = 5, Pomodoros = new List<Pomodoro>(), AvatarUrl = null },
            new() { Id = 2, UserName = "user2", ExpPoints = 800, Level = 4, Pomodoros = new List<Pomodoro>(), AvatarUrl = null },
            new() { Id = 3, UserName = "user3", ExpPoints = 600, Level = 3, Pomodoros = new List<Pomodoro>(), AvatarUrl = null }
        };

        var service = BuildService(users);

        // Act
        var result = await service.GetLeaderboardAsync(limit: 2);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Entries.Count);
        Assert.Equal("user1", result.Entries[0].Username);
        Assert.Equal("user2", result.Entries[1].Username);
    }

    [Fact]
    public async Task GetLeaderboardAsync_ReturnsEmpty_WhenNoUsers()
    {
        // Arrange
        var users = new List<AppUser>();
        var service = BuildService(users);

        // Act
        var result = await service.GetLeaderboardAsync(limit: 10);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task GetLeaderboardAsync_SortsBy_ExpPoints()
    {
        // Arrange
        var users = new List<AppUser>
        {
            new() { Id = 1, UserName = "user1", ExpPoints = 500, Level = 1, Pomodoros = new List<Pomodoro>(), AvatarUrl = null },
            new() { Id = 2, UserName = "user2", ExpPoints = 1000, Level = 2, Pomodoros = new List<Pomodoro>(), AvatarUrl = null },
            new() { Id = 3, UserName = "user3", ExpPoints = 750, Level = 3, Pomodoros = new List<Pomodoro>(), AvatarUrl = null }
        };

        var service = BuildService(users);

        // Act
        var result = await service.GetLeaderboardAsync(limit: 10);

        // Assert
        Assert.Equal(1000, result.Entries[0].Score);
        Assert.Equal(750, result.Entries[1].Score);
        Assert.Equal(500, result.Entries[2].Score);
    }

    [Fact]
    public async Task GetLeaderboardBySortAsync_SortsByXP()
    {
        // Arrange
        var users = new List<AppUser>
        {
            new() { Id = 1, UserName = "user1", ExpPoints = 500, Level = 1, Pomodoros = new List<Pomodoro>(), AvatarUrl = null },
            new() { Id = 2, UserName = "user2", ExpPoints = 1000, Level = 2, Pomodoros = new List<Pomodoro>(), AvatarUrl = null },
            new() { Id = 3, UserName = "user3", ExpPoints = 750, Level = 3, Pomodoros = new List<Pomodoro>(), AvatarUrl = null }
        };

        var service = BuildService(users);

        // Act
        var result = await service.GetLeaderboardBySortAsync(sortBy: "xp", limit: 10);

        // Assert
        Assert.Equal(3, result.Entries.Count);
        Assert.Equal(1000, result.Entries[0].Score);
        Assert.Equal(750, result.Entries[1].Score);
        Assert.Equal(500, result.Entries[2].Score);
    }

    [Fact]
    public async Task GetLeaderboardBySortAsync_SortsByPomodoro()
    {
        // Arrange
        var users = new List<AppUser>
        {
            new() { Id = 1, UserName = "user1", ExpPoints = 500, Level = 1, Pomodoros = new List<Pomodoro> { new() { Id = 1 } }, AvatarUrl = null },
            new() { Id = 2, UserName = "user2", ExpPoints = 1000, Level = 2, Pomodoros = new List<Pomodoro> { new() { Id = 2 }, new() { Id = 3 }, new() { Id = 4 } }, AvatarUrl = null },
            new() { Id = 3, UserName = "user3", ExpPoints = 750, Level = 3, Pomodoros = new List<Pomodoro> { new() { Id = 5 }, new() { Id = 6 } }, AvatarUrl = null }
        };

        var service = BuildService(users);

        // Act
        var result = await service.GetLeaderboardBySortAsync(sortBy: "pomodoro", limit: 10);

        // Assert
        Assert.Equal(3, result.Entries.Count);
        Assert.Equal(3, result.Entries[0].PomodoroSessions); // user2
        Assert.Equal(2, result.Entries[1].PomodoroSessions); // user3
        Assert.Equal(1, result.Entries[2].PomodoroSessions); // user1
    }

    [Fact]
    public async Task GetLeaderboardBySortAsync_SortsByLevel()
    {
        // Arrange
        var users = new List<AppUser>
        {
            new() { Id = 1, UserName = "user1", ExpPoints = 500, Level = 1, Pomodoros = new List<Pomodoro>(), AvatarUrl = null },
            new() { Id = 2, UserName = "user2", ExpPoints = 1000, Level = 5, Pomodoros = new List<Pomodoro>(), AvatarUrl = null },
            new() { Id = 3, UserName = "user3", ExpPoints = 750, Level = 3, Pomodoros = new List<Pomodoro>(), AvatarUrl = null }
        };

        var service = BuildService(users);

        // Act
        var result = await service.GetLeaderboardBySortAsync(sortBy: "level", limit: 10);

        // Assert
        Assert.Equal(3, result.Entries.Count);
        Assert.Equal(5, result.Entries[0].Level); // user2
        Assert.Equal(3, result.Entries[1].Level); // user3
        Assert.Equal(1, result.Entries[2].Level); // user1
    }

    [Fact]
    public async Task SearchLeaderboardAsync_FindsUserByName()
    {
        // Arrange
        var users = new List<AppUser>
        {
            new() { Id = 1, UserName = "john_doe", ExpPoints = 500, Level = 1, Pomodoros = new List<Pomodoro>(), AvatarUrl = null },
            new() { Id = 2, UserName = "jane_smith", ExpPoints = 1000, Level = 2, Pomodoros = new List<Pomodoro>(), AvatarUrl = null },
            new() { Id = 3, UserName = "john_smith", ExpPoints = 750, Level = 3, Pomodoros = new List<Pomodoro>(), AvatarUrl = null }
        };

        var service = BuildService(users);

        // Act
        var result = await service.SearchLeaderboardAsync(searchTerm: "john", limit: 50);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Entries.Count);
        Assert.Contains(result.Entries, e => e.Username == "john_doe");
        Assert.Contains(result.Entries, e => e.Username == "john_smith");
    }

    [Fact]
    public async Task SearchLeaderboardAsync_CaseInsensitiveSearch()
    {
        // Arrange
        var users = new List<AppUser>
        {
            new() { Id = 1, UserName = "JohnDoe", ExpPoints = 500, Level = 1, Pomodoros = new List<Pomodoro>(), AvatarUrl = null },
            new() { Id = 2, UserName = "janedoe", ExpPoints = 1000, Level = 2, Pomodoros = new List<Pomodoro>(), AvatarUrl = null }
        };

        var service = BuildService(users);

        // Act
        var result = await service.SearchLeaderboardAsync(searchTerm: "johndoe", limit: 50);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Entries);
        Assert.Equal("JohnDoe", result.Entries[0].Username);
    }

    [Fact]
    public async Task SearchLeaderboardAsync_ReturnsEmpty_WhenNoMatch()
    {
        // Arrange
        var users = new List<AppUser>
        {
            new() { Id = 1, UserName = "john_doe", ExpPoints = 500, Level = 1, Pomodoros = new List<Pomodoro>(), AvatarUrl = null },
            new() { Id = 2, UserName = "jane_smith", ExpPoints = 1000, Level = 2, Pomodoros = new List<Pomodoro>(), AvatarUrl = null }
        };

        var service = BuildService(users);

        // Act
        var result = await service.SearchLeaderboardAsync(searchTerm: "notexist", limit: 50);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Entries);
    }

    [Fact]
    public async Task SearchLeaderboardAsync_ReturnsLeaderboard_WhenSearchTermIsEmpty()
    {
        // Arrange
        var users = new List<AppUser>
        {
            new() { Id = 1, UserName = "user1", ExpPoints = 500, Level = 1, Pomodoros = new List<Pomodoro>(), AvatarUrl = null },
            new() { Id = 2, UserName = "user2", ExpPoints = 1000, Level = 2, Pomodoros = new List<Pomodoro>(), AvatarUrl = null }
        };

        var service = BuildService(users);

        // Act
        var result = await service.SearchLeaderboardAsync(searchTerm: "", limit: 50);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Entries.Count);
    }

    [Fact]
    public async Task GetFilteredAndSortedLeaderboardAsync_FiltersBysearchAndSortsbyXP()
    {
        // Arrange
        var users = new List<AppUser>
        {
            new() { Id = 1, UserName = "john_doe", ExpPoints = 500, Level = 1, Pomodoros = new List<Pomodoro>(), AvatarUrl = null },
            new() { Id = 2, UserName = "jane_smith", ExpPoints = 1000, Level = 2, Pomodoros = new List<Pomodoro>(), AvatarUrl = null },
            new() { Id = 3, UserName = "john_smith", ExpPoints = 750, Level = 3, Pomodoros = new List<Pomodoro>(), AvatarUrl = null }
        };

        var service = BuildService(users);

        // Act
        var result = await service.GetFilteredAndSortedLeaderboardAsync(searchTerm: "john", sortBy: "xp", limit: 50);

        // Assert
        Assert.Equal(2, result.Entries.Count);
        Assert.Equal("john_smith", result.Entries[0].Username); // 750 XP
        Assert.Equal("john_doe", result.Entries[1].Username);    // 500 XP
    }

    [Fact]
    public async Task GetFilteredAndSortedLeaderboardAsync_FiltersBysearchAndSortsByPomodoro()
    {
        // Arrange
        var users = new List<AppUser>
        {
            new() { Id = 1, UserName = "john_doe", ExpPoints = 500, Level = 1, Pomodoros = new List<Pomodoro> { new() { Id = 1 }, new() { Id = 2 } }, AvatarUrl = null },
            new() { Id = 2, UserName = "jane_smith", ExpPoints = 1000, Level = 2, Pomodoros = new List<Pomodoro> { new() { Id = 3 } }, AvatarUrl = null },
            new() { Id = 3, UserName = "john_smith", ExpPoints = 750, Level = 3, Pomodoros = new List<Pomodoro> { new() { Id = 4 }, new() { Id = 5 }, new() { Id = 6 } }, AvatarUrl = null }
        };

        var service = BuildService(users);

        // Act
        var result = await service.GetFilteredAndSortedLeaderboardAsync(searchTerm: "john", sortBy: "pomodoro", limit: 50);

        // Assert
        Assert.Equal(2, result.Entries.Count);
        Assert.Equal(3, result.Entries[0].PomodoroSessions); // john_smith
        Assert.Equal(2, result.Entries[1].PomodoroSessions); // john_doe
    }

    [Fact]
    public async Task GetFilteredAndSortedLeaderboardAsync_DefaultsToXP_WhenSortByInvalid()
    {
        // Arrange
        var users = new List<AppUser>
        {
            new() { Id = 1, UserName = "user1", ExpPoints = 500, Level = 1, Pomodoros = new List<Pomodoro>(), AvatarUrl = null },
            new() { Id = 2, UserName = "user2", ExpPoints = 1000, Level = 2, Pomodoros = new List<Pomodoro>(), AvatarUrl = null }
        };

        var service = BuildService(users);

        // Act
        var result = await service.GetFilteredAndSortedLeaderboardAsync(searchTerm: "", sortBy: "invalid", limit: 50);

        // Assert
        Assert.Equal(1000, result.Entries[0].Score); // Sorted by XP (default)
        Assert.Equal(500, result.Entries[1].Score);
    }

    [Fact]
    public async Task GetLeaderboardAsync_HandlesNullAvatar_WithDefaultValue()
    {
        // Arrange
        var users = new List<AppUser>
        {
            new() { Id = 1, UserName = "user1", ExpPoints = 1000, Level = 5, Pomodoros = new List<Pomodoro>(), AvatarUrl = null }
        };

        var service = BuildService(users);

        // Act
        var result = await service.GetLeaderboardAsync(limit: 10);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Entries);
        Assert.Null(result.Entries[0].AvatarUrl);
    }

    [Fact]
    public async Task GetLeaderboardAsync_HandlesNullUsername_WithDefaultValue()
    {
        // Arrange
        var users = new List<AppUser>
        {
            new() { Id = 1, UserName = null, ExpPoints = 1000, Level = 5, Pomodoros = new List<Pomodoro>(), AvatarUrl = "avatar.jpg" }
        };

        var service = BuildService(users);

        // Act
        var result = await service.GetLeaderboardAsync(limit: 10);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Entries);
        Assert.Equal("Невідомий користувач", result.Entries[0].Username);
    }

    [Fact]
    public async Task GetLeaderboardAsync_SetCorrectRanks()
    {
        // Arrange
        var users = new List<AppUser>
        {
            new() { Id = 1, UserName = "user1", ExpPoints = 1000, Level = 5, Pomodoros = new List<Pomodoro>(), AvatarUrl = null },
            new() { Id = 2, UserName = "user2", ExpPoints = 800, Level = 4, Pomodoros = new List<Pomodoro>(), AvatarUrl = null },
            new() { Id = 3, UserName = "user3", ExpPoints = 600, Level = 3, Pomodoros = new List<Pomodoro>(), AvatarUrl = null }
        };

        var service = BuildService(users);

        // Act
        var result = await service.GetLeaderboardAsync(limit: 10);

        // Assert
        Assert.Equal(1, result.Entries[0].Rank);
        Assert.Equal(2, result.Entries[1].Rank);
        Assert.Equal(3, result.Entries[2].Rank);
    }
}

