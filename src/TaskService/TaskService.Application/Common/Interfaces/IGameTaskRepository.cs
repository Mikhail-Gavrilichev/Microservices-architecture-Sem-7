using TaskService.Domain.GameTasks;

namespace TaskService.Application.Common.Interfaces;

public interface IGameTaskRepository
{
    Task<GameTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(GameTask task, CancellationToken cancellationToken);

    Task<(IReadOnlyList<GameTask> Items, int TotalCount)> ListAsync(
        GameTaskStatus? status,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
