using MediatR;
using ReviewService.Application.ReviewRequests.Dtos;

namespace ReviewService.Application.ReviewRequests.Commands.AssignNextReview;

/// <summary>
/// Политика «назначить следующую работу доступному члену жюри»:
/// берёт самый старый запрос из очереди и назначает его указанному члену жюри.
/// </summary>
public sealed record AssignNextReviewCommand(Guid JuryMemberId) : IRequest<JuryReviewDto?>;
