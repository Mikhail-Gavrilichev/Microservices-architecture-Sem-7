using Microsoft.EntityFrameworkCore;
using TaskService.Application.Common.Interfaces;
using TaskService.Domain.GameTasks;

namespace TaskService.Infrastructure.Persistence.Repositories;

public sealed class GameTaskRepository(TaskDbContext dbContext) : IGameTaskRepository
{
    public Task<GameTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Tasks.FirstOrDefaultAsync(task => task.Id == id, cancellationToken);

    public async Task AddAsync(GameTask task, CancellationToken cancellationToken) =>
        await dbContext.Tasks.AddAsync(task, cancellationToken);

    public async Task<(IReadOnlyList<GameTask> Items, int TotalCount)> ListAsync(
        GameTaskStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Tasks.AsNoTracking().AsQueryable();

        if (status is not null)
        {
            query = query.Where(task => task.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = search.Trim().ToLowerInvariant();
            query = query.Where(task =>
                task.Title.ToLower().Contains(normalizedSearch) ||
                task.Statement.ToLower().Contains(normalizedSearch));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(task => task.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
