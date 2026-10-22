using FluentValidation;

namespace ReviewService.Application.ReviewRequests.Commands.AssignNextReview;

public sealed class AssignNextReviewCommandValidator : AbstractValidator<AssignNextReviewCommand>
{
    public AssignNextReviewCommandValidator()
    {
        RuleFor(command => command.JuryMemberId).NotEmpty();
    }
}
