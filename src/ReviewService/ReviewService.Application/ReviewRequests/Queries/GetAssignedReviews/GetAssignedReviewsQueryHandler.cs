using MediatR;
using ReviewService.Application.Common.Interfaces;
using ReviewService.Application.ReviewRequests.Dtos;

namespace ReviewService.Application.ReviewRequests.Queries.GetAssignedReviews;

public sealed class GetAssignedReviewsQueryHandler(IReviewRequestRepository repository)
    : IRequestHandler<GetAssignedReviewsQuery, IReadOnlyList<JuryReviewDto>>
{
    public async Task<IReadOnlyList<JuryReviewDto>> Handle(
        GetAssignedReviewsQuery request,
        CancellationToken cancellationToken)
    {
        var items = await repository.GetAssignedToAsync(request.JuryMemberId, cancellationToken);
        return items.Select(JuryReviewDto.FromDomain).ToArray();
    }
}
