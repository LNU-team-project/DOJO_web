using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Data;
using DOJO2.Application.Common;
using DOJO2.Application.Interfaces;
using DOJO2.Application.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DOJO2.Infrastructure.Services;

public class PlanService : IPlanService
{
    private static class PriorityLevels
    {
        public const int Low = 1;
        public const int Medium = 2;
        public const int High = 3;
    }

    private const string PlanNotFoundMsg = "План не знайдено";
    private const string SubTaskNotFoundMsg = "Підзадачу не знайдено";
    private const string SubTaskTitleRequiredMsg = "Назва підзадачі не може бути порожньою";
    private const string SubTaskTitleTooLongMsg = "Назва підзадачі не може перевищувати 255 символів";
    private const string InvalidFileMsg = "Оберіть файл для завантаження";
    private const string FileTooLargeMsg = "Розмір файлу не може перевищувати 10MB";
    private const string InvalidFileTypeMsg = "Недопустимий тип файлу";
    private const string AttachmentNotFoundMsg = "Вкладення не знайдено";
    private const int MaxTaskTitleLength = 255;
    private const int MaxFileNameLength = 255;
    private const long MaxAttachmentSizeInBytes = 10 * 1024 * 1024;
    private const string PlanAttachmentDirectory = "uploads/plan-attachments";
    private static readonly string[] AllowedAttachmentExtensions =
    {
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".png", ".jpg", ".jpeg", ".webp"
    };

    private readonly IAppDbContext _context;
    private readonly ILogger<PlanService> _logger;

    public PlanService(IAppDbContext context, ILogger<PlanService>? logger = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _logger = logger ?? NullLogger<PlanService>.Instance;
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

        if (model.Title.Length > MaxTaskTitleLength)
        {
            return Result<PlanItemViewModel>.FailureResult($"Назва плану не може перевищувати {MaxTaskTitleLength} символів");
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
            .Where(t => t.UserId == userId && t.IsPlan && t.ParentTaskId == null)
            .OrderBy(t => t.ScheduledAt)
            .ToListAsync();

        var planIds = plans.Select(p => p.Id).ToList();
        var subTaskCounts = await _context.Tasks
            .Where(t => t.ParentTaskId != null && planIds.Contains(t.ParentTaskId.Value))
            .GroupBy(t => t.ParentTaskId!.Value)
            .Select(g => new { PlanId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PlanId, x => x.Count);

        var vm = new PlanListViewModel
        {
            IncompletePlans = plans.Where(p => !p.IsCompleted).Select(p => MapToViewModel(p, subTaskCounts.GetValueOrDefault(p.Id, 0))).ToList(),
            CompletedPlans = plans.Where(p => p.IsCompleted).OrderByDescending(p => p.CompletedAt).Select(p => MapToViewModel(p, subTaskCounts.GetValueOrDefault(p.Id, 0))).ToList()
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

        var subTaskCount = await _context.Tasks.CountAsync(t => t.ParentTaskId == plan.Id);
        return Result<PlanItemViewModel>.SuccessResult(MapToViewModel(plan, subTaskCount), "План отримано");
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

        if (model.Title.Length > MaxTaskTitleLength)
            return Result<PlanItemViewModel>.FailureResult($"Назва плану не може перевищувати {MaxTaskTitleLength} символів");

        if (model.ScheduledAt == null)
            return Result<PlanItemViewModel>.FailureResult("Оберіть дату та час плану");

        plan.Title = model.Title.Trim();
        plan.Description = model.Description?.Trim();
        plan.Priority = model.Priority;
        plan.ScheduledAt = model.ScheduledAt;
        // TaskItem doesn't have UpdatedAt column in the domain model; rely on DB triggers or UpdatedAt in other tables if needed

        _context.Tasks.Update(plan);
        await _context.SaveChangesAsync();

        var subTaskCount = await _context.Tasks.CountAsync(t => t.ParentTaskId == plan.Id);

        return Result<PlanItemViewModel>.SuccessResult(MapToViewModel(plan, subTaskCount), "План оновлено");
    }

    public async Task<Result<PlanAttachmentItemViewModel>> UploadPlanAttachmentAsync(int planId, int userId, FileUploadData? file)
    {
        var plan = await GetUserPlanAsync(planId, userId);
        if (plan == null)
        {
            return Result<PlanAttachmentItemViewModel>.FailureResult(PlanNotFoundMsg);
        }

        if (file == null || file.Length <= 0 || file.Content.Length == 0)
        {
            return Result<PlanAttachmentItemViewModel>.FailureResult(InvalidFileMsg);
        }

        if (file.Length > MaxAttachmentSizeInBytes)
        {
            return Result<PlanAttachmentItemViewModel>.FailureResult(FileTooLargeMsg);
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedAttachmentExtensions.Contains(extension))
        {
            return Result<PlanAttachmentItemViewModel>.FailureResult(InvalidFileTypeMsg);
        }

        var uploadRootPath = EnsurePlanAttachmentDirectory();
        var generatedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullFilePath = Path.Combine(uploadRootPath, generatedFileName);

        await File.WriteAllBytesAsync(fullFilePath, file.Content);

        var attachment = new Attachment
        {
            TaskId = plan.Id,
            FileName = TruncateFileName(Path.GetFileName(file.FileName)),
            FilePath = $"/{PlanAttachmentDirectory}/{generatedFileName}",
            CreatedAt = DateTime.UtcNow
        };

        _context.Attachments.Add(attachment);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Вкладення {AttachmentId} додано до плану {PlanId}", attachment.Id, planId);

        return Result<PlanAttachmentItemViewModel>.SuccessResult(
            MapToAttachmentViewModel(attachment),
            "Файл успішно прикріплено");
    }

    public async Task<Result<List<PlanAttachmentItemViewModel>>> GetPlanAttachmentsAsync(int planId, int userId)
    {
        var plan = await GetUserPlanAsync(planId, userId);
        if (plan == null)
        {
            return Result<List<PlanAttachmentItemViewModel>>.FailureResult(PlanNotFoundMsg);
        }

        var attachments = await _context.Attachments
            .AsNoTracking()
            .Where(a => a.TaskId == planId)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new PlanAttachmentItemViewModel
            {
                Id = a.Id,
                FileName = a.FileName,
                FileUrl = a.FilePath,
                CreatedAt = a.CreatedAt
            })
            .ToListAsync();

        return Result<List<PlanAttachmentItemViewModel>>.SuccessResult(attachments, "Вкладення завантажено");
    }

    public async Task<Result<bool>> DeletePlanAttachmentAsync(int planId, int attachmentId, int userId)
    {
        var plan = await GetUserPlanAsync(planId, userId);
        if (plan == null)
        {
            return Result<bool>.FailureResult(PlanNotFoundMsg);
        }

        var attachment = await _context.Attachments
            .FirstOrDefaultAsync(a => a.Id == attachmentId && a.TaskId == planId);

        if (attachment == null)
        {
            return Result<bool>.FailureResult(AttachmentNotFoundMsg);
        }

        var filePath = attachment.FilePath;

        _context.Attachments.Remove(attachment);
        await _context.SaveChangesAsync();

        TryDeleteAttachmentFile(filePath);
        _logger.LogInformation("Вкладення {AttachmentId} видалено з плану {PlanId}", attachmentId, planId);

        return Result<bool>.SuccessResult(true, "Вкладення видалено");
    }

    public async Task<Result<List<PlanSubTaskItemViewModel>>> GetPlanSubTasksAsync(int planId, int userId)
    {
        var plan = await GetUserPlanAsync(planId, userId);
        if (plan == null)
        {
            return Result<List<PlanSubTaskItemViewModel>>.FailureResult(PlanNotFoundMsg);
        }

        var subTasks = await _context.Tasks
            .AsNoTracking()
            .Where(t => t.UserId == userId && t.ParentTaskId == planId)
            .OrderBy(t => t.IsCompleted)
            .ThenBy(t => t.CreatedAt)
            .Select(t => new PlanSubTaskItemViewModel
            {
                Id = t.Id,
                ParentPlanId = t.ParentTaskId ?? 0,
                Title = t.Title,
                IsCompleted = t.IsCompleted,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return Result<List<PlanSubTaskItemViewModel>>.SuccessResult(subTasks, "Підзадачі завантажено");
    }

    public async Task<Result<PlanSubTaskItemViewModel>> CreatePlanSubTaskAsync(int planId, int userId, PlanSubTaskCreateViewModel? model)
    {
        var plan = await GetUserPlanAsync(planId, userId);
        if (plan == null)
        {
            return Result<PlanSubTaskItemViewModel>.FailureResult(PlanNotFoundMsg);
        }

        var validationResult = ValidateSubTaskModel(model);
        if (validationResult != null)
        {
            return validationResult;
        }

        var subTask = new TaskItem
        {
            UserId = userId,
            ParentTaskId = plan.Id,
            IsPlan = false,
            IsCompleted = false,
            Title = model!.Title.Trim(),
            CreatedAt = DateTime.UtcNow,
            Priority = plan.Priority
        };

        _context.Tasks.Add(subTask);
        await _context.SaveChangesAsync();

        return Result<PlanSubTaskItemViewModel>.SuccessResult(MapToSubTaskViewModel(subTask), "Підзадачу додано");
    }

    public async Task<Result<PlanSubTaskItemViewModel>> UpdatePlanSubTaskAsync(int planId, int subTaskId, int userId, PlanSubTaskCreateViewModel? model)
    {
        var plan = await GetUserPlanAsync(planId, userId);
        if (plan == null)
        {
            return Result<PlanSubTaskItemViewModel>.FailureResult(PlanNotFoundMsg);
        }

        var validationResult = ValidateSubTaskModel(model);
        if (validationResult != null)
        {
            return validationResult;
        }

        var subTask = await GetPlanSubTaskAsync(planId, subTaskId, userId);
        if (subTask == null)
        {
            return Result<PlanSubTaskItemViewModel>.FailureResult(SubTaskNotFoundMsg);
        }

        subTask.Title = model!.Title.Trim();

        _context.Tasks.Update(subTask);
        await _context.SaveChangesAsync();

        return Result<PlanSubTaskItemViewModel>.SuccessResult(MapToSubTaskViewModel(subTask), "Підзадачу оновлено");
    }

    public async Task<Result<bool>> TogglePlanSubTaskStatusAsync(int planId, int subTaskId, int userId, bool isCompleted)
    {
        var plan = await GetUserPlanAsync(planId, userId);
        if (plan == null)
        {
            return Result<bool>.FailureResult(PlanNotFoundMsg);
        }

        var subTask = await GetPlanSubTaskAsync(planId, subTaskId, userId);
        if (subTask == null)
        {
            return Result<bool>.FailureResult(SubTaskNotFoundMsg);
        }

        if (subTask.IsCompleted == isCompleted)
        {
            return Result<bool>.SuccessResult(true, "Статус підзадачі не змінився");
        }

        subTask.IsCompleted = isCompleted;
        subTask.CompletedAt = isCompleted ? DateTime.UtcNow : null;

        _context.Tasks.Update(subTask);
        await _context.SaveChangesAsync();

        return Result<bool>.SuccessResult(true, isCompleted ? "Підзадачу виконано" : "Підзадачу повернуто в активні");
    }

    public async Task<Result<bool>> DeletePlanSubTaskAsync(int planId, int subTaskId, int userId)
    {
        var plan = await GetUserPlanAsync(planId, userId);
        if (plan == null)
        {
            return Result<bool>.FailureResult(PlanNotFoundMsg);
        }

        var subTask = await GetPlanSubTaskAsync(planId, subTaskId, userId);
        if (subTask == null)
        {
            return Result<bool>.FailureResult(SubTaskNotFoundMsg);
        }

        _context.Tasks.Remove(subTask);
        await _context.SaveChangesAsync();

        return Result<bool>.SuccessResult(true, "Підзадачу видалено");
    }

    private async Task<TaskItem?> GetUserPlanAsync(int planId, int userId)
    {
        return await _context.Tasks.FirstOrDefaultAsync(p => p.Id == planId && p.UserId == userId && p.IsPlan && p.ParentTaskId == null);
    }

    private async Task<TaskItem?> GetPlanSubTaskAsync(int planId, int subTaskId, int userId)
    {
        return await _context.Tasks.FirstOrDefaultAsync(t =>
            t.Id == subTaskId &&
            t.UserId == userId &&
            t.ParentTaskId == planId);
    }

    private static Result<PlanSubTaskItemViewModel>? ValidateSubTaskModel(PlanSubTaskCreateViewModel? model)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.Title))
        {
            return Result<PlanSubTaskItemViewModel>.FailureResult(SubTaskTitleRequiredMsg);
        }

        if (model.Title.Length > MaxTaskTitleLength)
        {
            return Result<PlanSubTaskItemViewModel>.FailureResult(SubTaskTitleTooLongMsg);
        }

        return null;
    }

    private static PlanItemViewModel MapToViewModel(TaskItem plan, int subTaskCount = 0)
    {
        return new PlanItemViewModel
        {
            Id = plan.Id,
            Title = plan.Title,
            Description = plan.Description,
            ScheduledAt = plan.ScheduledAt,
            Priority = plan.Priority,
            IsCompleted = plan.IsCompleted,
            PriorityLabel = GetPriorityLabel(plan.Priority),
            HasSubTasks = subTaskCount > 0,
            SubTaskCount = subTaskCount
        };
    }

    private static PlanSubTaskItemViewModel MapToSubTaskViewModel(TaskItem subTask)
    {
        return new PlanSubTaskItemViewModel
        {
            Id = subTask.Id,
            ParentPlanId = subTask.ParentTaskId ?? 0,
            Title = subTask.Title,
            IsCompleted = subTask.IsCompleted,
            CreatedAt = subTask.CreatedAt
        };
    }

    private static PlanAttachmentItemViewModel MapToAttachmentViewModel(Attachment attachment)
    {
        return new PlanAttachmentItemViewModel
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            FileUrl = attachment.FilePath,
            CreatedAt = attachment.CreatedAt
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

    private static string TruncateFileName(string fileName)
    {
        if (fileName.Length <= MaxFileNameLength)
        {
            return fileName;
        }

        return fileName.Substring(0, MaxFileNameLength);
    }

    private static string EnsurePlanAttachmentDirectory()
    {
        var absolutePath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "Presentation",
            "wwwroot",
            "uploads",
            "plan-attachments");

        if (!Directory.Exists(absolutePath))
        {
            Directory.CreateDirectory(absolutePath);
        }

        return absolutePath;
    }

    private void TryDeleteAttachmentFile(string? fileUrl)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
        {
            return;
        }

        var uploadRootPath = EnsurePlanAttachmentDirectory();
        var rootPath = Path.GetFullPath(uploadRootPath);
        var fileName = Path.GetFileName(fileUrl);
        var candidatePath = Path.GetFullPath(Path.Combine(uploadRootPath, fileName));

        if (!candidatePath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Пропущено видалення файла вкладення поза дозволеною директорією");
            return;
        }

        if (!File.Exists(candidatePath))
        {
            return;
        }

        try
        {
            File.Delete(candidatePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не вдалося видалити файл вкладення {FilePath}", candidatePath);
        }
    }
}
