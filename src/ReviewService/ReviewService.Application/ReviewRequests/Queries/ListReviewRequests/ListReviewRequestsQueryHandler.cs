using MediatR;
using ReviewService.Application.Common.Interfaces;
using ReviewService.Application.Common.Models;
using ReviewService.Application.ReviewRequests.Dtos;

namespace ReviewService.Application.ReviewRequests.Queries.ListReviewRequests;

public sealed class ListReviewRequestsQueryHandler(IReviewRequestRepository repository)
    : IRequestHandler<ListReviewRequestsQuery, PagedResult<ReviewRequestDetailsDto>>
{
    public async Task<PagedResult<ReviewRequestDetailsDto>> Handle(
        ListReviewRequestsQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await repository.ListAsync(
            request.Status,
            request.Page,
            request.PageSize,
            cancellationToken);

        return new PagedResult<ReviewRequestDetailsDto>(
            items.Select(ReviewRequestDetailsDto.FromDomain).ToArray(),
            request.Page,
            request.PageSize,
            totalCount);
    }
}
