using MediatR;
using TaskService.Application.Common.Exceptions;
using TaskService.Application.Common.Interfaces;
using TaskService.Application.GameTasks.Dtos;
using TaskService.Domain.GameTasks;

namespace TaskService.Application.GameTasks.Queries.GetTaskSnapshot;

public sealed class GetTaskSnapshotQueryHandler(IGameTaskRepository repository)
    : IRequestHandler<GetTaskSnapshotQuery, TaskSnapshotDto>
{
    public async Task<TaskSnapshotDto> Handle(
        GetTaskSnapshotQuery request,
        CancellationToken cancellationToken)
    {
        var task = await repository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException($"Задача '{request.TaskId}' не найдена.");

        if (task.Status != GameTaskStatus.Published || task.PublishedAt is null)
        {
            throw new ConflictException($"Задача '{request.TaskId}' ещё не опубликована.");
        }

        return new TaskSnapshotDto(
            task.Id,
            task.Statement,
            task.CorrectAnswer,
            task.PublishedAt.Value);
    }
}
