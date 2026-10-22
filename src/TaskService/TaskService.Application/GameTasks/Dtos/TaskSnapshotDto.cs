namespace TaskService.Application.GameTasks.Dtos;

/// <summary>
/// Контракт для других сервисов (в частности, ReviewService).
/// Отдаётся только для опубликованной задачи и содержит снимок условия и правильного ответа.
/// </summary>
public sealed record TaskSnapshotDto(
    Guid TaskId,
    string Statement,
    string CorrectAnswer,
    DateTimeOffset PublishedAt);
