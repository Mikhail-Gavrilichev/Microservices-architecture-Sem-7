using FluentValidation;

namespace ReviewService.Application.ReviewRequests.Queries.GetJuryReview;

public sealed class GetJuryReviewQueryValidator : AbstractValidator<GetJuryReviewQuery>
{
    public GetJuryReviewQueryValidator()
    {
        RuleFor(query => query.ReviewRequestId).NotEmpty();
        RuleFor(query => query.JuryMemberId).NotEmpty();
    }
}
