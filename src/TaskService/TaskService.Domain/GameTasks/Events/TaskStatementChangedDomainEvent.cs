using TaskService.Domain.Common;

namespace TaskService.Domain.GameTasks.Events;

public sealed record TaskStatementChangedDomainEvent(
    Guid TaskId,
    DateTimeOffset OccurredAt) : IDomainEvent;
