using ReviewService.Domain.ReviewRequests;

namespace ReviewService.Api.Contracts;

public sealed record CreateReviewRequestRequest(
    Guid SolutionId,
    Guid TaskId,
    string ParticipantAnswer,
    Guid TeamId,
    ReviewSource Source = ReviewSource.Solution,
    Guid? SourceReferenceId = null);

public sealed record AssignReviewRequest(Guid JuryMemberId);

public sealed record RefuseReviewRequest(Guid JuryMemberId, string Reason);

public sealed record TransferReviewRequest(Guid SeniorJuryMemberId);

public sealed record CompleteReviewRequest(
    Guid JuryMemberId,
    ReviewVerdict Verdict,
    string? Comment = null);
