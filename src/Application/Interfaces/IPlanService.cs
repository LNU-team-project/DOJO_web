using DOJO2.Application.ViewModels;
using DOJO2.Application.Common;

namespace DOJO2.Application.Interfaces;

public interface IPlanService
{
    Task<Result<PlanItemViewModel>> CreatePlanAsync(int userId, PlanCreateViewModel? model);
    Task<Result<PlanListViewModel>> GetUserPlansAsync(int userId);
    Task<Result<bool>> MarkPlanAsCompletedAsync(int planId, int userId);
    Task<Result<bool>> MarkPlanAsIncompleteAsync(int planId, int userId);
    Task<Result<bool>> DeletePlanAsync(int planId, int userId);
    Task<Result<PlanItemViewModel>> GetPlanByIdAsync(int planId, int userId);
    Task<Result<PlanItemViewModel>> UpdatePlanAsync(int planId, int userId, PlanCreateViewModel? model);
    Task<Result<PlanAttachmentItemViewModel>> UploadPlanAttachmentAsync(int planId, int userId, FileUploadData? file);
    Task<Result<List<PlanAttachmentItemViewModel>>> GetPlanAttachmentsAsync(int planId, int userId);
    Task<Result<bool>> DeletePlanAttachmentAsync(int planId, int attachmentId, int userId);
    Task<Result<List<PlanSubTaskItemViewModel>>> GetPlanSubTasksAsync(int planId, int userId);
    Task<Result<PlanSubTaskItemViewModel>> CreatePlanSubTaskAsync(int planId, int userId, PlanSubTaskCreateViewModel? model);
    Task<Result<PlanSubTaskItemViewModel>> UpdatePlanSubTaskAsync(int planId, int subTaskId, int userId, PlanSubTaskCreateViewModel? model);
    Task<Result<bool>> TogglePlanSubTaskStatusAsync(int planId, int subTaskId, int userId, bool isCompleted);
    Task<Result<bool>> DeletePlanSubTaskAsync(int planId, int subTaskId, int userId);
}
