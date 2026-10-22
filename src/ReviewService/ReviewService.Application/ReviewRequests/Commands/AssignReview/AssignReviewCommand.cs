using MediatR;

namespace ReviewService.Application.ReviewRequests.Commands.AssignReview;

/// <summary>Команда «Назначить проверку».</summary>
public sealed record AssignReviewCommand(Guid ReviewRequestId, Guid JuryMemberId) : IRequest<Unit>;
