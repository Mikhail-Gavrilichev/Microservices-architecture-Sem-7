using ReviewService.Domain.ReviewRequests;

namespace ReviewService.Application.ReviewRequests.Dtos;

/// <summary>
/// Элемент очереди. Не содержит команду, решение и рейтинг.
/// </summary>
public sealed record ReviewQueueItemDto(
    Guid Id,
    Guid TaskId,
    string TaskStatement,
    DateTimeOffset CreatedAt,
    DateTimeOffset? QueuedAt)
{
    public static ReviewQueueItemDto FromDomain(ReviewRequest request) => new(
        request.Id,
        request.TaskId,
        request.TaskStatement,
        request.CreatedAt,
        request.QueuedAt);
}
