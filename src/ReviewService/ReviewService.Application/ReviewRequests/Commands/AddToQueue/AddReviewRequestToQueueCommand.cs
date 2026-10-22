using MediatR;

namespace ReviewService.Application.ReviewRequests.Commands.AddToQueue;

/// <summary>Команда «Добавить в очередь».</summary>
public sealed record AddReviewRequestToQueueCommand(Guid ReviewRequestId) : IRequest<Unit>;
