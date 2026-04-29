using System;
using System.Linq;
using System.Threading.Tasks;
using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Data;
using DOJO2.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace DOJO_web.Tests;

public class FriendServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static UserManager<AppUser> BuildUserManager(AppDbContext context)
    {
        var store = new UserStore<AppUser, IdentityRole<int>, AppDbContext, int>(context);
        var options = new OptionsWrapper<IdentityOptions>(new IdentityOptions());
        var passwordHasher = new PasswordHasher<AppUser>();
        var userValidators = new[] { new UserValidator<AppUser>() };
        var passwordValidators = new[] { new PasswordValidator<AppUser>() };
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var services = new ServiceCollection().BuildServiceProvider();
        var logger = new Logger<UserManager<AppUser>>(new LoggerFactory());

        return new UserManager<AppUser>(
            store,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            errors,
            services,
            logger);
    }

    private static UserService BuildService(AppDbContext context, UserManager<AppUser> userManager)
    {
        var logger = new Mock<ILogger<UserService>>();
        var env = new Mock<IWebHostEnvironment>();
        return new UserService(userManager, logger.Object, env.Object, context);
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
        using var context = CreateContext();
        var userManager = BuildUserManager(context);
        var service = BuildService(context, userManager);

        var result = await service.AddFriendAsync(1, 1);

        Assert.False(result.Success);
        Assert.Contains("себе", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddFriendAsync_SavesFriend_WhenUserExists()
    {
        using var context = CreateContext();
        context.Users.Add(BuildUser(2, "friend", "friend@example.com"));
        await context.SaveChangesAsync();

        var userManager = BuildUserManager(context);
        var service = BuildService(context, userManager);

        var result = await service.AddFriendAsync(1, 2);

        Assert.True(result.Success);
        var saved = await context.Friends.FirstOrDefaultAsync(f => f.UserId == 1 && f.FriendUserId == 2);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task AddFriendAsync_ReturnsFailure_WhenDuplicate()
    {
        using var context = CreateContext();
        context.Users.Add(BuildUser(3, "friend", "friend3@example.com"));
        context.Friends.Add(new Friend { UserId = 1, FriendUserId = 3 });
        await context.SaveChangesAsync();

        var userManager = BuildUserManager(context);
        var service = BuildService(context, userManager);

        var result = await service.AddFriendAsync(1, 3);

        Assert.False(result.Success);
        Assert.Contains("в списку", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddFriendByUserNameAsync_ReturnsFailure_WhenUserMissing()
    {
        using var context = CreateContext();
        var userManager = BuildUserManager(context);
        var service = BuildService(context, userManager);

        var result = await service.AddFriendByUserNameAsync(1, "ghost");

        Assert.False(result.Success);
        Assert.Contains("не знайдено", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AddFriendByUserNameAsync_Succeeds_WhenUserExists()
    {
        using var context = CreateContext();
        context.Users.Add(BuildUser(5, "Friend", "friend5@example.com"));
        await context.SaveChangesAsync();

        var userManager = BuildUserManager(context);
        var service = BuildService(context, userManager);

        var result = await service.AddFriendByUserNameAsync(1, "Friend");

        Assert.True(result.Success);
        Assert.NotNull(await context.FriendRequests.FirstOrDefaultAsync(fr => fr.RequesterUserId == 1 && fr.ReceiverUserId == 5));
        Assert.Null(await context.Friends.FirstOrDefaultAsync(f => f.UserId == 1 && f.FriendUserId == 5));
    }

    [Fact]
    public async Task AcceptFriendRequestAsync_CreatesMutualFriendship()
    {
        using var context = CreateContext();
        context.Users.Add(BuildUser(5, "Friend", "friend5@example.com"));
        context.Users.Add(BuildUser(1, "Owner", "owner@example.com"));
        context.FriendRequests.Add(new FriendRequest
        {
            Id = 10,
            RequesterUserId = 5,
            ReceiverUserId = 1,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var userManager = BuildUserManager(context);
        var service = BuildService(context, userManager);

        var result = await service.AcceptFriendRequestAsync(1, 10);

        Assert.True(result.Success);
        Assert.NotNull(await context.Friends.FirstOrDefaultAsync(f => f.UserId == 1 && f.FriendUserId == 5));
        Assert.NotNull(await context.Friends.FirstOrDefaultAsync(f => f.UserId == 5 && f.FriendUserId == 1));
        Assert.Null(await context.FriendRequests.FirstOrDefaultAsync(fr => fr.Id == 10));
    }

    [Fact]
    public async Task RemoveFriendAsync_RemovesExistingEntry()
    {
        using var context = CreateContext();
        context.Friends.Add(new Friend { UserId = 1, FriendUserId = 9 });
        await context.SaveChangesAsync();

        var userManager = BuildUserManager(context);
        var service = BuildService(context, userManager);

        var result = await service.RemoveFriendAsync(1, 9);

        Assert.True(result.Success);
        var deleted = await context.Friends.FirstOrDefaultAsync(f => f.UserId == 1 && f.FriendUserId == 9);
        Assert.Null(deleted);
    }
}
