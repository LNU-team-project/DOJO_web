using DOJO2.Application.Interfaces;
using DOJO2.Domain.Entities;
using DOJO2.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace DOJO2.Infrastructure.Repositories;

public class PomodoroPresetRepository : IPomodoroPresetRepository
{
    private readonly IAppDbContext _context;

    public PomodoroPresetRepository(IAppDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<PomodoroPreset>> GetUserPresetsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.PomodoroPresets
            .AsNoTracking()
            .Where(preset => preset.UserId == userId)
            .OrderBy(preset => preset.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<PomodoroPreset?> GetUserPresetAsync(int userId, int presetId, CancellationToken cancellationToken = default)
    {
        return await _context.PomodoroPresets
            .FirstOrDefaultAsync(preset => preset.UserId == userId && preset.Id == presetId, cancellationToken);
    }

    public Task<bool> HasPresetNameAsync(int userId, string normalizedName, CancellationToken cancellationToken = default)
    {
        return _context.PomodoroPresets.AnyAsync(
            preset => preset.UserId == userId && preset.Name.ToUpper() == normalizedName,
            cancellationToken);
    }

    public async Task<PomodoroPreset> AddAsync(PomodoroPreset preset, CancellationToken cancellationToken = default)
    {
        await _context.PomodoroPresets.AddAsync(preset, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return preset;
    }

    public async Task DeleteAsync(PomodoroPreset preset, CancellationToken cancellationToken = default)
    {
        _context.PomodoroPresets.Remove(preset);
        await _context.SaveChangesAsync(cancellationToken);
    }
}