using MediatR;

namespace ReviewService.Application.ReviewRequests.Commands.AcceptReview;

/// <summary>Команда «Принять проверку».</summary>
public sealed record AcceptReviewCommand(Guid ReviewRequestId, Guid JuryMemberId) : IRequest<Unit>;
