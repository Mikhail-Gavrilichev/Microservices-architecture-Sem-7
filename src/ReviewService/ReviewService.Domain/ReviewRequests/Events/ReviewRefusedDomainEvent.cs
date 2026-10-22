using ReviewService.Domain.Common;

namespace ReviewService.Domain.ReviewRequests.Events;

public sealed record ReviewRefusedDomainEvent(
    Guid ReviewRequestId,
    Guid JuryMemberId,
    string Reason,
    DateTimeOffset OccurredAt) : IDomainEvent;
