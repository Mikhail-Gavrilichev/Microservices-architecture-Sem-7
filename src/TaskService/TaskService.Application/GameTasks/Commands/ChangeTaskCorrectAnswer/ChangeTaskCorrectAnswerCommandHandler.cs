using MediatR;
using TaskService.Application.Common.Exceptions;
using TaskService.Application.Common.Interfaces;

namespace TaskService.Application.GameTasks.Commands.ChangeTaskCorrectAnswer;

public sealed class ChangeTaskCorrectAnswerCommandHandler(
    IGameTaskRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<ChangeTaskCorrectAnswerCommand, Unit>
{
    public async Task<Unit> Handle(ChangeTaskCorrectAnswerCommand request, CancellationToken cancellationToken)
    {
        var task = await repository.GetByIdAsync(request.TaskId, cancellationToken)
            ?? throw new NotFoundException($"Задача '{request.TaskId}' не найдена.");

        task.ChangeCorrectAnswer(request.CorrectAnswer, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
