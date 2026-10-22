using ReviewService.Domain.ReviewRequests;

namespace ReviewService.Application.ReviewRequests.Dtos;

/// <summary>
/// Представление для жюри. Намеренно НЕ содержит TeamId, SolutionId, рейтинг
/// и любые другие сведения, позволяющие идентифицировать команду.
/// </summary>
public sealed record JuryReviewDto(
    Guid Id,
    Guid TaskId,
    string TaskStatement,
    string ParticipantAnswer,
    string CorrectAnswer,
    ReviewStatus Status,
    ReviewVerdict? Verdict,
    string? Comment,
    DateTimeOffset CreatedAt,
    DateTimeOffset? AssignedAt,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset? CompletedAt)
{
    public static JuryReviewDto FromDomain(ReviewRequest request) => new(
        request.Id,
        request.TaskId,
        request.TaskStatement,
        request.ParticipantAnswer,
        request.CorrectAnswer,
        request.Status,
        request.Verdict,
        request.Comment,
        request.CreatedAt,
        request.AssignedAt,
        request.AcceptedAt,
        request.CompletedAt);
}
