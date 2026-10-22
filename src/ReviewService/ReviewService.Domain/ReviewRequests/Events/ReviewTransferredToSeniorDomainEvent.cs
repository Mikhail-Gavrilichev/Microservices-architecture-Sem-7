using ReviewService.Domain.Common;

namespace ReviewService.Domain.ReviewRequests.Events;

public sealed record ReviewTransferredToSeniorDomainEvent(
    Guid ReviewRequestId,
    Guid SeniorJuryMemberId,
    DateTimeOffset OccurredAt) : IDomainEvent;
