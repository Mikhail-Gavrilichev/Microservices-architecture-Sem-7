using ReviewService.Application.Common.Interfaces;

namespace ReviewService.Infrastructure.Persistence;

public sealed class UnitOfWork(ReviewDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
