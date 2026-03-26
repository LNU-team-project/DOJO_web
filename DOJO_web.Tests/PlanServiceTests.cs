using System;
using System.Threading.Tasks;
using DOJO2.Infrastructure.Data;
using DOJO2.Infrastructure.Services;
using DOJO2.Presentation.ViewModels;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DOJO_web.Tests;

public class PlanServiceTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreatePlanAsync_ReturnsFailure_WhenModelIsNull()
    {
        using var context = CreateContext();
        var service = new PlanService(context);

        var result = await service.CreatePlanAsync(1, null);

        Assert.False(result.Success);
        Assert.Contains("не може бути порожною", result.Message);
    }

    [Fact]
    public async Task CreatePlanAsync_ReturnsFailure_WhenTitleEmpty()
    {
        using var context = CreateContext();
        var service = new PlanService(context);
        var model = new PlanCreateViewModel { Title = "   ", ScheduledAt = DateTime.UtcNow };

        var result = await service.CreatePlanAsync(1, model);

        Assert.False(result.Success);
        Assert.Contains("Назва плану не може бути порожньою", result.Message);
    }

    [Fact]
    public async Task CreatePlanAsync_ReturnsFailure_WhenScheduledAtMissing()
    {
        using var context = CreateContext();
        var service = new PlanService(context);
        var model = new PlanCreateViewModel { Title = "Plan", ScheduledAt = null };

        var result = await service.CreatePlanAsync(1, model);

        Assert.False(result.Success);
        Assert.Contains("Оберіть дату та час плану", result.Message);
    }

    [Fact]
    public async Task CreatePlanAsync_Succeeds_AndPersistsPlan()
    {
        using var context = CreateContext();
        var service = new PlanService(context);
        var now = DateTime.UtcNow;
        var model = new PlanCreateViewModel
        {
            Title = "Test plan",
            Description = "Desc",
            Priority = 2,
            ScheduledAt = now
        };

        var result = await service.CreatePlanAsync(7, model);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Test plan", result.Data!.Title);
        Assert.Equal(2, result.Data.Priority);
        Assert.Equal(now, result.Data.ScheduledAt);

        var saved = await context.Tasks.FirstOrDefaultAsync(t => t.Id == result.Data.Id);
        Assert.NotNull(saved);
        Assert.Equal(7, saved!.UserId);
        Assert.True(saved.IsPlan);
    }

    [Fact]
    public async Task GetUserPlansAsync_ReturnsSeparatedLists()
    {
        using var context = CreateContext();
        context.Tasks.AddRange(
            new DOJO2.Domain.Entities.TaskItem { UserId = 1, Title = "active", IsPlan = true, ScheduledAt = DateTime.UtcNow, IsCompleted = false },
            new DOJO2.Domain.Entities.TaskItem { UserId = 1, Title = "done", IsPlan = true, ScheduledAt = DateTime.UtcNow, IsCompleted = true, CompletedAt = DateTime.UtcNow }
        );
        await context.SaveChangesAsync();

        var service = new PlanService(context);
        var result = await service.GetUserPlansAsync(1);

        Assert.True(result.Success);
        Assert.Single(result.Data!.IncompletePlans);
        Assert.Single(result.Data.CompletedPlans);
        Assert.Equal("active", result.Data.IncompletePlans[0].Title);
        Assert.Equal("done", result.Data.CompletedPlans[0].Title);
    }

    [Fact]
    public async Task MarkPlanAsCompleted_SetsFlags()
    {
        using var context = CreateContext();
        var plan = new DOJO2.Domain.Entities.TaskItem { UserId = 1, Title = "p", IsPlan = true, IsCompleted = false };
        context.Tasks.Add(plan);
        await context.SaveChangesAsync();

        var service = new PlanService(context);
        var result = await service.MarkPlanAsCompletedAsync(plan.Id, 1);

        Assert.True(result.Success);
        var updated = await context.Tasks.FindAsync(plan.Id);
        Assert.True(updated!.IsCompleted);
        Assert.NotNull(updated.CompletedAt);
    }

    [Fact]
    public async Task MarkPlanAsIncomplete_SetsFlags()
    {
        using var context = CreateContext();
        var plan = new DOJO2.Domain.Entities.TaskItem { UserId = 1, Title = "p", IsPlan = true, IsCompleted = true, CompletedAt = DateTime.UtcNow };
        context.Tasks.Add(plan);
        await context.SaveChangesAsync();

        var service = new PlanService(context);
        var result = await service.MarkPlanAsIncompleteAsync(plan.Id, 1);

        Assert.True(result.Success);
        var updated = await context.Tasks.FindAsync(plan.Id);
        Assert.False(updated!.IsCompleted);
        Assert.Null(updated.CompletedAt);
    }

    [Fact]
    public async Task DeletePlanAsync_RemovesEntity()
    {
        using var context = CreateContext();
        var plan = new DOJO2.Domain.Entities.TaskItem { UserId = 1, Title = "p", IsPlan = true };
        context.Tasks.Add(plan);
        await context.SaveChangesAsync();

        var service = new PlanService(context);
        var result = await service.DeletePlanAsync(plan.Id, 1);

        Assert.True(result.Success);
        var deleted = await context.Tasks.FindAsync(plan.Id);
        Assert.Null(deleted);
    }
}

