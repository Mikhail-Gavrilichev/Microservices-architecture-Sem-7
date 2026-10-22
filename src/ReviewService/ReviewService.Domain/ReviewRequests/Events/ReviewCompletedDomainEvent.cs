using ReviewService.Domain.Common;

namespace ReviewService.Domain.ReviewRequests.Events;

public sealed record ReviewCompletedDomainEvent(
    Guid ReviewRequestId,
    Guid TeamId,
    Guid JuryMemberId,
    ReviewVerdict Verdict,
    DateTimeOffset OccurredAt) : IDomainEvent;
