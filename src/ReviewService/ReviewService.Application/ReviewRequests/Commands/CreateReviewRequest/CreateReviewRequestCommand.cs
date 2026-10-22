using MediatR;
using ReviewService.Application.ReviewRequests.Dtos;
using ReviewService.Domain.ReviewRequests;

namespace ReviewService.Application.ReviewRequests.Commands.CreateReviewRequest;

/// <summary>
/// Команда «Создать запрос на проверку».
/// Вызывается после того, как решение прошло проверку фотографии, либо по апелляции.
/// </summary>
public sealed record CreateReviewRequestCommand(
    Guid SolutionId,
    Guid TaskId,
    string ParticipantAnswer,
    Guid TeamId,
    ReviewSource Source = ReviewSource.Solution,
    Guid? SourceReferenceId = null) : IRequest<ReviewRequestDetailsDto>;
