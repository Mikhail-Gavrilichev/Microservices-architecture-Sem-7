using MediatR;
using ReviewService.Application.ReviewRequests.Dtos;

namespace ReviewService.Application.ReviewRequests.Queries.GetAssignedReviews;

public sealed record GetAssignedReviewsQuery(Guid JuryMemberId) : IRequest<IReadOnlyList<JuryReviewDto>>;
