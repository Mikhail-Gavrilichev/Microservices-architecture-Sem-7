using ReviewService.Domain.Common;

namespace ReviewService.Domain.ReviewRequests.Events;

public sealed record ReviewAcceptedDomainEvent(
    Guid ReviewRequestId,
    Guid JuryMemberId,
    DateTimeOffset OccurredAt) : IDomainEvent;
