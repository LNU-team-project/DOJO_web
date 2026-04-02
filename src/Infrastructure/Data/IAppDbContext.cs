using DOJO2.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DOJO2.Infrastructure.Data;

public interface IAppDbContext
{
    DbSet<TaskItem> Tasks { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

