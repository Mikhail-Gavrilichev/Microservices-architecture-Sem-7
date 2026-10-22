using FluentValidation;

namespace ReviewService.Application.ReviewRequests.Commands.AssignReview;

public sealed class AssignReviewCommandValidator : AbstractValidator<AssignReviewCommand>
{
    public AssignReviewCommandValidator()
    {
        RuleFor(command => command.ReviewRequestId).NotEmpty();
        RuleFor(command => command.JuryMemberId).NotEmpty();
    }
}
