using MediatR;
using ReviewService.Application.Common.Models;
using ReviewService.Application.ReviewRequests.Dtos;
using ReviewService.Domain.ReviewRequests;

namespace ReviewService.Application.ReviewRequests.Queries.ListReviewRequests;

public sealed record ListReviewRequestsQuery(
    ReviewStatus? Status = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<ReviewRequestDetailsDto>>;
