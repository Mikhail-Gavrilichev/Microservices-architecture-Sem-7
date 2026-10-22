using ReviewService.Domain.ReviewRequests;

namespace ReviewService.Application.ReviewRequests.Dtos;

/// <summary>
/// Полное представление запроса для смотрителя/внутренних интеграций.
/// Содержит TeamId, поэтому НЕ должно использоваться в jury-эндпоинтах.
/// </summary>
public sealed record ReviewRequestDetailsDto(
    Guid Id,
    Guid SolutionId,
    Guid TaskId,
    string TaskStatement,
    string CorrectAnswer,
    string ParticipantAnswer,
    Guid TeamId,
    ReviewSource Source,
    Guid? SourceReferenceId,
    ReviewStatus Status,
    Guid? AssignedJuryMemberId,
    string? RefusalReason,
    ReviewVerdict? Verdict,
    string? Comment,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? QueuedAt,
    DateTimeOffset? AssignedAt,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset? RefusedAt,
    DateTimeOffset? TransferredAt,
    DateTimeOffset? CompletedAt)
{
    public static ReviewRequestDetailsDto FromDomain(ReviewRequest request) => new(
        request.Id,
        request.SolutionId,
        request.TaskId,
        request.TaskStatement,
        request.CorrectAnswer,
        request.ParticipantAnswer,
        request.TeamId,
        request.Source,
        request.SourceReferenceId,
        request.Status,
        request.AssignedJuryMemberId,
        request.RefusalReason,
        request.Verdict,
        request.Comment,
        request.CreatedAt,
        request.UpdatedAt,
        request.QueuedAt,
        request.AssignedAt,
        request.AcceptedAt,
        request.RefusedAt,
        request.TransferredAt,
        request.CompletedAt);
}
