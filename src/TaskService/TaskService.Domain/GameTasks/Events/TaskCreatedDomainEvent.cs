using TaskService.Domain.Common;

namespace TaskService.Domain.GameTasks.Events;

public sealed record TaskCreatedDomainEvent(
    Guid TaskId,
    string Title,
    DateTimeOffset OccurredAt) : IDomainEvent;
