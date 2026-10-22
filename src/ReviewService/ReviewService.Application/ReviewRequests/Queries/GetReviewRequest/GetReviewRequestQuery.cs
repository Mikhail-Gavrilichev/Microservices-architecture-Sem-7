using MediatR;
using ReviewService.Application.ReviewRequests.Dtos;

namespace ReviewService.Application.ReviewRequests.Queries.GetReviewRequest;

public sealed record GetReviewRequestQuery(Guid ReviewRequestId) : IRequest<ReviewRequestDetailsDto>;
