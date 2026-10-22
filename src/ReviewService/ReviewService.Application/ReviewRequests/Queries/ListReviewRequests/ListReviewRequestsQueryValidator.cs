using FluentValidation;

namespace ReviewService.Application.ReviewRequests.Queries.ListReviewRequests;

public sealed class ListReviewRequestsQueryValidator : AbstractValidator<ListReviewRequestsQuery>
{
    public ListReviewRequestsQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
    }
}
