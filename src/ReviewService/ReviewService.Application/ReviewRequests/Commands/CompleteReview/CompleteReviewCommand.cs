using MediatR;
using ReviewService.Domain.ReviewRequests;

namespace ReviewService.Application.ReviewRequests.Commands.CompleteReview;

/// <summary>Команда «Завершить проверку».</summary>
public sealed record CompleteReviewCommand(
    Guid ReviewRequestId,
    Guid JuryMemberId,
    ReviewVerdict Verdict,
    string? Comment = null) : IRequest<Unit>;
