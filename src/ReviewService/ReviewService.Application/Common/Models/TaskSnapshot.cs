namespace ReviewService.Application.Common.Models;

/// <summary>
/// Снимок опубликованной задачи, полученный из TaskService.
/// </summary>
public sealed record TaskSnapshot(
    Guid TaskId,
    string Statement,
    string CorrectAnswer,
    DateTimeOffset PublishedAt);
