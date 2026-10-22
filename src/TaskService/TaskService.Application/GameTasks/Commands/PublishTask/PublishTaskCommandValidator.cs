using FluentValidation;

namespace TaskService.Application.GameTasks.Commands.PublishTask;

public sealed class PublishTaskCommandValidator : AbstractValidator<PublishTaskCommand>
{
    public PublishTaskCommandValidator()
    {
        RuleFor(command => command.TaskId).NotEmpty();
    }
}
