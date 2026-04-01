using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Data;
using DOJO2.Infrastructure.Results;
using DOJO2.Presentation.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace DOJO2.Infrastructure.Services;

public interface IPlanService
{
    Task<Result<PlanItemViewModel>> CreatePlanAsync(int userId, PlanCreateViewModel? model);
    Task<Result<PlanListViewModel>> GetUserPlansAsync(int userId);
    Task<Result<bool>> MarkPlanAsCompletedAsync(int planId, int userId);
    Task<Result<bool>> MarkPlanAsIncompleteAsync(int planId, int userId);
    Task<Result<bool>> DeletePlanAsync(int planId, int userId);
    Task<Result<PlanItemViewModel>> GetPlanByIdAsync(int planId, int userId);
    Task<Result<PlanItemViewModel>> UpdatePlanAsync(int planId, int userId, PlanCreateViewModel? model);
}

public class PlanService : IPlanService
{
    private static class PriorityLevels
    {
        public const int Low = 1;
        public const int Medium = 2;
        public const int High = 3;
    }

    private const string PlanNotFoundMsg = "План не знайдено";

    private readonly AppDbContext _context;

    public PlanService(AppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Result<PlanItemViewModel>> CreatePlanAsync(int userId, PlanCreateViewModel? model)
    {
        if (model == null)
        {
            return Result<PlanItemViewModel>.FailureResult("Модель плану не може бути порожною");
        }

        if (string.IsNullOrWhiteSpace(model.Title))
        {
            return Result<PlanItemViewModel>.FailureResult("Назва плану не може бути порожньою");
        }

        if (model.Title.Length > 255)
        {
            return Result<PlanItemViewModel>.FailureResult("Назва плану не може перевищувати 255 символів");
        }

        if (model.ScheduledAt == null)
        {
            return Result<PlanItemViewModel>.FailureResult("Оберіть дату та час плану");
        }

        var plan = new TaskItem
        {
            UserId = userId,
            Title = model.Title.Trim(),
            Description = model.Description?.Trim(),
            Priority = model.Priority,
            IsPlan = true,
            ScheduledAt = model.ScheduledAt,
            IsCompleted = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(plan);
        await _context.SaveChangesAsync();

        return Result<PlanItemViewModel>.SuccessResult(MapToViewModel(plan), "План створено");
    }

    public async Task<Result<PlanListViewModel>> GetUserPlansAsync(int userId)
    {
        var plans = await _context.Tasks
            .Where(t => t.UserId == userId && t.IsPlan)
            .OrderBy(t => t.ScheduledAt)
            .ToListAsync();

        var vm = new PlanListViewModel
        {
            IncompletePlans = plans.Where(p => !p.IsCompleted).Select(MapToViewModel).ToList(),
            CompletedPlans = plans.Where(p => p.IsCompleted).OrderByDescending(p => p.CompletedAt).Select(MapToViewModel).ToList()
        };

        return Result<PlanListViewModel>.SuccessResult(vm, "Плани отримано");
    }

    public async Task<Result<bool>> MarkPlanAsCompletedAsync(int planId, int userId)
    {
        var plan = await GetUserPlanAsync(planId, userId);
        if (plan == null)
        {
            return Result<bool>.FailureResult(PlanNotFoundMsg);
        }

        if (plan.IsCompleted)
        {
            return Result<bool>.FailureResult("План вже виконаний");
        }

        plan.IsCompleted = true;
        plan.CompletedAt = DateTime.UtcNow;
        _context.Tasks.Update(plan);
        await _context.SaveChangesAsync();
        return Result<bool>.SuccessResult(true, "План позначено виконаним");
    }

    public async Task<Result<bool>> MarkPlanAsIncompleteAsync(int planId, int userId)
    {
        var plan = await GetUserPlanAsync(planId, userId);
        if (plan == null)
        {
            return Result<bool>.FailureResult(PlanNotFoundMsg);
        }

        if (!plan.IsCompleted)
        {
            return Result<bool>.FailureResult("План уже невиконаний");
        }

        plan.IsCompleted = false;
        plan.CompletedAt = null;
        _context.Tasks.Update(plan);
        await _context.SaveChangesAsync();
        return Result<bool>.SuccessResult(true, "План повернуто до активних");
    }

    public async Task<Result<bool>> DeletePlanAsync(int planId, int userId)
    {
        var plan = await GetUserPlanAsync(planId, userId);
        if (plan == null)
        {
            return Result<bool>.FailureResult(PlanNotFoundMsg);
        }

        _context.Tasks.Remove(plan);
        await _context.SaveChangesAsync();
        return Result<bool>.SuccessResult(true, "План видалено");
    }

    public async Task<Result<PlanItemViewModel>> GetPlanByIdAsync(int planId, int userId)
    {
        var plan = await GetUserPlanAsync(planId, userId);
        if (plan == null)
            return Result<PlanItemViewModel>.FailureResult(PlanNotFoundMsg);

        return Result<PlanItemViewModel>.SuccessResult(MapToViewModel(plan), "План отримано");
    }

    public async Task<Result<PlanItemViewModel>> UpdatePlanAsync(int planId, int userId, PlanCreateViewModel? model)
    {
        if (model == null)
            return Result<PlanItemViewModel>.FailureResult("Модель плану не може бути порожною");

        var plan = await GetUserPlanAsync(planId, userId);
        if (plan == null)
            return Result<PlanItemViewModel>.FailureResult(PlanNotFoundMsg);

        if (string.IsNullOrWhiteSpace(model.Title))
            return Result<PlanItemViewModel>.FailureResult("Назва плану не може бути порожньою");

        if (model.Title.Length > 255)
            return Result<PlanItemViewModel>.FailureResult("Назва плану не може перевищувати 255 символів");

        if (model.ScheduledAt == null)
            return Result<PlanItemViewModel>.FailureResult("Оберіть дату та час плану");

        plan.Title = model.Title.Trim();
        plan.Description = model.Description?.Trim();
        plan.Priority = model.Priority;
        plan.ScheduledAt = model.ScheduledAt;
        // TaskItem doesn't have UpdatedAt column in the domain model; rely on DB triggers or UpdatedAt in other tables if needed

        _context.Tasks.Update(plan);
        await _context.SaveChangesAsync();

        return Result<PlanItemViewModel>.SuccessResult(MapToViewModel(plan), "План оновлено");
    }

    private async Task<TaskItem?> GetUserPlanAsync(int planId, int userId)
    {
        return await _context.Tasks.FirstOrDefaultAsync(p => p.Id == planId && p.UserId == userId && p.IsPlan);
    }

    private static PlanItemViewModel MapToViewModel(TaskItem plan)
    {
        return new PlanItemViewModel
        {
            Id = plan.Id,
            Title = plan.Title,
            Description = plan.Description,
            ScheduledAt = plan.ScheduledAt,
            Priority = plan.Priority,
            IsCompleted = plan.IsCompleted,
            PriorityLabel = GetPriorityLabel(plan.Priority)
        };
    }

    private static string GetPriorityLabel(int priority)
    {
        return priority switch
        {
            PriorityLevels.Low => "Низька",
            PriorityLevels.Medium => "Середня",
            PriorityLevels.High => "Висока",
            _ => "Невідома"
        };
    }
}
