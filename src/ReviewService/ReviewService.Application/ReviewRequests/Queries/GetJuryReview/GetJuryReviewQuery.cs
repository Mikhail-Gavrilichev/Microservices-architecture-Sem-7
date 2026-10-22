using MediatR;
using ReviewService.Application.ReviewRequests.Dtos;

namespace ReviewService.Application.ReviewRequests.Queries.GetJuryReview;

/// <summary>
/// Просмотр конкретной проверки жюри. Возвращает NotFound, если проверка
/// не назначена этому члену жюри, чтобы не раскрывать существование запроса.
/// </summary>
public sealed record GetJuryReviewQuery(
    Guid ReviewRequestId,
    Guid JuryMemberId) : IRequest<JuryReviewDto>;
