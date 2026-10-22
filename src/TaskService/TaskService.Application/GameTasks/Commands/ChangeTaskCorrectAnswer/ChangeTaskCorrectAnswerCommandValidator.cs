using FluentValidation;
using TaskService.Domain.GameTasks;

namespace TaskService.Application.GameTasks.Commands.ChangeTaskCorrectAnswer;

public sealed class ChangeTaskCorrectAnswerCommandValidator : AbstractValidator<ChangeTaskCorrectAnswerCommand>
{
    public ChangeTaskCorrectAnswerCommandValidator()
    {
        RuleFor(command => command.TaskId).NotEmpty();

        RuleFor(command => command.CorrectAnswer)
            .NotEmpty().WithMessage("Правильный ответ обязателен.")
            .MaximumLength(GameTask.CorrectAnswerMaxLength);
    }
}
