using DOJO2.Application.ViewModels;
using DOJO2.Infrastructure.Results;
using Microsoft.AspNetCore.Http;

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
    Task<Result<PlanAttachmentItemViewModel>> UploadPlanAttachmentAsync(int planId, int userId, IFormFile? file);
    Task<Result<List<PlanAttachmentItemViewModel>>> GetPlanAttachmentsAsync(int planId, int userId);
    Task<Result<bool>> DeletePlanAttachmentAsync(int planId, int attachmentId, int userId);
}
