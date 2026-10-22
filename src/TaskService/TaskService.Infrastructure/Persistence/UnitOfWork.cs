using TaskService.Application.Common.Interfaces;

namespace TaskService.Infrastructure.Persistence;

public sealed class UnitOfWork(TaskDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
