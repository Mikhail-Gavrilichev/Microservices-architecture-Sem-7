using FluentValidation;
using TaskService.Domain.GameTasks;

namespace TaskService.Application.GameTasks.Commands.ChangeTaskStatement;

public sealed class ChangeTaskStatementCommandValidator : AbstractValidator<ChangeTaskStatementCommand>
{
    public ChangeTaskStatementCommandValidator()
    {
        RuleFor(command => command.TaskId).NotEmpty();

        RuleFor(command => command.Statement)
            .NotEmpty().WithMessage("Условие задачи обязательно.")
            .MaximumLength(GameTask.StatementMaxLength);
    }
}
