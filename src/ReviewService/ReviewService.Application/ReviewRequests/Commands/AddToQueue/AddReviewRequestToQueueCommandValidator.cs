using FluentValidation;

namespace ReviewService.Application.ReviewRequests.Commands.AddToQueue;

public sealed class AddReviewRequestToQueueCommandValidator
    : AbstractValidator<AddReviewRequestToQueueCommand>
{
    public AddReviewRequestToQueueCommandValidator()
    {
        RuleFor(command => command.ReviewRequestId).NotEmpty();
    }
}
