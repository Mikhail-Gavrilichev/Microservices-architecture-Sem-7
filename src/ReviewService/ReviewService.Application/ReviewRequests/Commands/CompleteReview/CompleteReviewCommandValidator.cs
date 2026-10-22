using FluentValidation;
using ReviewService.Domain.ReviewRequests;

namespace ReviewService.Application.ReviewRequests.Commands.CompleteReview;

public sealed class CompleteReviewCommandValidator : AbstractValidator<CompleteReviewCommand>
{
    public CompleteReviewCommandValidator()
    {
        RuleFor(command => command.ReviewRequestId).NotEmpty();
        RuleFor(command => command.JuryMemberId).NotEmpty();
        RuleFor(command => command.Verdict).IsInEnum();

        RuleFor(command => command.Comment)
            .MaximumLength(ReviewRequest.CommentMaxLength);
    }
}
