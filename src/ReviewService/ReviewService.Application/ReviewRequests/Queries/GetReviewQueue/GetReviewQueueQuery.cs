using MediatR;
using ReviewService.Application.ReviewRequests.Dtos;

namespace ReviewService.Application.ReviewRequests.Queries.GetReviewQueue;

public sealed record GetReviewQueueQuery(int Limit = 50) : IRequest<IReadOnlyList<ReviewQueueItemDto>>;
