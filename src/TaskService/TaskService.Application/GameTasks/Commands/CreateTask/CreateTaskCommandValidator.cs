using FluentValidation;
using TaskService.Domain.GameTasks;

namespace TaskService.Application.GameTasks.Commands.CreateTask;

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(command => command.Title)
            .NotEmpty().WithMessage("Название задачи обязательно.")
            .MaximumLength(GameTask.TitleMaxLength);

        RuleFor(command => command.Statement)
            .MaximumLength(GameTask.StatementMaxLength);

        RuleFor(command => command.CorrectAnswer)
            .MaximumLength(GameTask.CorrectAnswerMaxLength);
    }
}
