using FluentValidation;
using ReviewService.Domain.ReviewRequests;

namespace ReviewService.Application.ReviewRequests.Commands.CreateReviewRequest;

public sealed class CreateReviewRequestCommandValidator : AbstractValidator<CreateReviewRequestCommand>
{
    public CreateReviewRequestCommandValidator()
    {
        RuleFor(command => command.SolutionId).NotEmpty();
        RuleFor(command => command.TaskId).NotEmpty();
        RuleFor(command => command.TeamId).NotEmpty();

        RuleFor(command => command.ParticipantAnswer)
            .NotEmpty().WithMessage("Ответ участника обязателен.")
            .MaximumLength(ReviewRequest.ParticipantAnswerMaxLength);

        RuleFor(command => command.Source).IsInEnum();

        RuleFor(command => command.SourceReferenceId)
            .NotNull()
            .WithMessage("Для повторной проверки по апелляции необходимо указать идентификатор апелляции.")
            .When(command => command.Source == ReviewSource.Appeal);
    }
}
