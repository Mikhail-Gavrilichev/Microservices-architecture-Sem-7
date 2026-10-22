using MediatR;
using ReviewService.Application.Common.Interfaces;
using ReviewService.Application.ReviewRequests.Dtos;

namespace ReviewService.Application.ReviewRequests.Queries.GetReviewQueue;

public sealed class GetReviewQueueQueryHandler(IReviewRequestRepository repository)
    : IRequestHandler<GetReviewQueueQuery, IReadOnlyList<ReviewQueueItemDto>>
{
    public async Task<IReadOnlyList<ReviewQueueItemDto>> Handle(
        GetReviewQueueQuery request,
        CancellationToken cancellationToken)
    {
        var items = await repository.GetQueueAsync(request.Limit, cancellationToken);
        return items.Select(ReviewQueueItemDto.FromDomain).ToArray();
    }
}
