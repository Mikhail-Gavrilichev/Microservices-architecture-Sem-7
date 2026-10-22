using FluentValidation;
using ReviewService.Domain.ReviewRequests;

namespace ReviewService.Application.ReviewRequests.Commands.RefuseReview;

public sealed class RefuseReviewCommandValidator : AbstractValidator<RefuseReviewCommand>
{
    public RefuseReviewCommandValidator()
    {
        RuleFor(command => command.ReviewRequestId).NotEmpty();
        RuleFor(command => command.JuryMemberId).NotEmpty();

        RuleFor(command => command.Reason)
            .NotEmpty().WithMessage("Причина отказа обязательна.")
            .MaximumLength(ReviewRequest.RefusalReasonMaxLength);
    }
}
