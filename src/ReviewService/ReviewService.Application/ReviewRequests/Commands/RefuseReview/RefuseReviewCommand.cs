using MediatR;

namespace ReviewService.Application.ReviewRequests.Commands.RefuseReview;

/// <summary>Команда «Отказаться от проверки».</summary>
public sealed record RefuseReviewCommand(
    Guid ReviewRequestId,
    Guid JuryMemberId,
    string Reason) : IRequest<Unit>;
