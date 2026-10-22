using ReviewService.Domain.Common;

namespace ReviewService.Domain.ReviewRequests.Events;

public sealed record ReviewRequestCreatedDomainEvent(
    Guid ReviewRequestId,
    Guid TaskId,
    Guid TeamId,
    ReviewSource Source,
    DateTimeOffset OccurredAt) : IDomainEvent;
