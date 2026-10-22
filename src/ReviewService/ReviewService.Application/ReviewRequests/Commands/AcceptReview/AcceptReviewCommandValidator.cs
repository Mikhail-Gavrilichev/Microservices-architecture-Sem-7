using FluentValidation;

namespace ReviewService.Application.ReviewRequests.Commands.AcceptReview;

public sealed class AcceptReviewCommandValidator : AbstractValidator<AcceptReviewCommand>
{
    public AcceptReviewCommandValidator()
    {
        RuleFor(command => command.ReviewRequestId).NotEmpty();
        RuleFor(command => command.JuryMemberId).NotEmpty();
    }
}
