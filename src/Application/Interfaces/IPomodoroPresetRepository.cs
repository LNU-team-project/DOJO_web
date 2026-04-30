using DOJO2.Domain.Entities;

namespace DOJO2.Application.Interfaces;

public interface IPomodoroPresetRepository
{
    Task<IReadOnlyList<PomodoroPreset>> GetUserPresetsAsync(int userId, CancellationToken cancellationToken = default);
    Task<PomodoroPreset?> GetUserPresetAsync(int userId, int presetId, CancellationToken cancellationToken = default);
    Task<bool> HasPresetNameAsync(int userId, string normalizedName, CancellationToken cancellationToken = default);
    Task<PomodoroPreset> AddAsync(PomodoroPreset preset, CancellationToken cancellationToken = default);
    Task DeleteAsync(PomodoroPreset preset, CancellationToken cancellationToken = default);
}