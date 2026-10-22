using ReviewService.Domain.Common;

namespace ReviewService.Domain.ReviewRequests.Events;

public sealed record ReviewAssignedDomainEvent(
    Guid ReviewRequestId,
    Guid JuryMemberId,
    DateTimeOffset OccurredAt) : IDomainEvent;
