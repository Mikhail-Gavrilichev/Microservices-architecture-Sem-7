using MediatR;
using TaskService.Application.Common.Exceptions;
using TaskService.Application.Common.Interfaces;

namespace TaskService.Application.GameTasks.Commands.ChangeTaskStatement;

public sealed class ChangeTaskStatementCommandHandler(
    IGameTaskRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<ChangeTaskStatementCommand, Unit>
{
    public async Task<Unit> Handle(ChangeTaskStatementCommand request, CancellationToken cancellationToken)
    {
        var task = await repository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException($"Задача '{request.TaskId}' не найдена.");

        task.ChangeStatement(request.Statement, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
