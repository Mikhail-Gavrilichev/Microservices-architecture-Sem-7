using TaskService.Domain.Common;

namespace TaskService.Domain.GameTasks.Events;

public sealed record TaskPublishedDomainEvent(
    Guid TaskId,
    DateTimeOffset OccurredAt) : IDomainEvent;
