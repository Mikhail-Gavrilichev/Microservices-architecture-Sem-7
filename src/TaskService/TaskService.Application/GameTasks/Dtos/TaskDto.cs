using TaskService.Domain.GameTasks;

namespace TaskService.Application.GameTasks.Dtos;

public sealed record TaskDto(
    Guid Id,
    string Title,
    string Statement,
    string CorrectAnswer,
    GameTaskStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt)
{
    public static TaskDto FromDomain(GameTask task) => new(
        task.Id,
        task.Title,
        task.Statement,
        task.CorrectAnswer,
        task.Status,
        task.CreatedAt,
        task.UpdatedAt,
        task.PublishedAt);
}
