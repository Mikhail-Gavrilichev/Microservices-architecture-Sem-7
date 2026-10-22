using TaskService.Domain.Common;

namespace TaskService.Domain.GameTasks.Events;

public sealed record TaskCorrectAnswerChangedDomainEvent(
    Guid TaskId,
    DateTimeOffset OccurredAt) : IDomainEvent;
