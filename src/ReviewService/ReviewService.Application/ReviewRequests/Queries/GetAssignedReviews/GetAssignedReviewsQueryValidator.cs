using FluentValidation;

namespace ReviewService.Application.ReviewRequests.Queries.GetAssignedReviews;

public sealed class GetAssignedReviewsQueryValidator : AbstractValidator<GetAssignedReviewsQuery>
{
    public GetAssignedReviewsQueryValidator()
    {
        RuleFor(query => query.JuryMemberId).NotEmpty();
    }
}
