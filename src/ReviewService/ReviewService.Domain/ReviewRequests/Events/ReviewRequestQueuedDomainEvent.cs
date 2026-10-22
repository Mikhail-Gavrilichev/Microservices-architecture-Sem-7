using ReviewService.Domain.Common;

namespace ReviewService.Domain.ReviewRequests.Events;

public sealed record ReviewRequestQueuedDomainEvent(
    Guid ReviewRequestId,
    DateTimeOffset OccurredAt) : IDomainEvent;
