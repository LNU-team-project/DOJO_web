using DOJO2.Application.ViewModels;
using DOJO2.Infrastructure.Results;

namespace DOJO2.Application.Interfaces;

public interface IHeroService
{
    Task<Result<HeroStatusViewModel>> GetHeroStatusAsync(int userId);
    Task<Result<HeroStatusViewModel>> AwardExpForTaskAsync(int taskId, int userId);
}
