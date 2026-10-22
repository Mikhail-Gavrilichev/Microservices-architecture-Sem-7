using MediatR;
using TaskService.Application.Common.Exceptions;
using TaskService.Application.Common.Interfaces;
using TaskService.Application.GameTasks.Dtos;

namespace TaskService.Application.GameTasks.Queries.GetTask;

public sealed class GetTaskQueryHandler(IGameTaskRepository repository)
    : IRequestHandler<GetTaskQuery, TaskDto>
{
    public async Task<TaskDto> Handle(GetTaskQuery request, CancellationToken cancellationToken)
    {
        var task = await repository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException($"Задача '{request.TaskId}' не найдена.");

        return TaskDto.FromDomain(task);
    }
}
