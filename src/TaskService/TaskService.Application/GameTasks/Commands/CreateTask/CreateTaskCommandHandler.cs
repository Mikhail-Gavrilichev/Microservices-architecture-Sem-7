using MediatR;
using TaskService.Application.Common.Interfaces;
using TaskService.Application.GameTasks.Dtos;
using TaskService.Domain.GameTasks;

namespace TaskService.Application.GameTasks.Commands.CreateTask;

public sealed class CreateTaskCommandHandler(
    IGameTaskRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<CreateTaskCommand, TaskDto>
{
    public async Task<TaskDto> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = GameTask.Create(
            request.Title,
            request.Statement,
            request.CorrectAnswer,
            timeProvider.GetUtcNow());

        await repository.AddAsync(task, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return TaskDto.FromDomain(task);
    }
}
