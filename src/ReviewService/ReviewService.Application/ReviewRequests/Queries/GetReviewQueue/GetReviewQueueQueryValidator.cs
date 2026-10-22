using FluentValidation;

namespace ReviewService.Application.ReviewRequests.Queries.GetReviewQueue;

public sealed class GetReviewQueueQueryValidator : AbstractValidator<GetReviewQueueQuery>
{
    public GetReviewQueueQueryValidator()
    {
        RuleFor(query => query.Limit).InclusiveBetween(1, 100);
    }
}
