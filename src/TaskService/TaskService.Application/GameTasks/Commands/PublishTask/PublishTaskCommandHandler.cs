using MediatR;
using TaskService.Application.Common.Exceptions;
using TaskService.Application.Common.Interfaces;

namespace TaskService.Application.GameTasks.Commands.PublishTask;

public sealed class PublishTaskCommandHandler(
    IGameTaskRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<PublishTaskCommand, Unit>
{
    public async Task<Unit> Handle(PublishTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await repository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException($"Задача '{request.TaskId}' не найдена.");

        task.Publish(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
