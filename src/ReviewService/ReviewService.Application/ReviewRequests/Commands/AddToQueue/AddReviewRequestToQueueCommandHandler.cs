using MediatR;
using ReviewService.Application.Common.Exceptions;
using ReviewService.Application.Common.Interfaces;

namespace ReviewService.Application.ReviewRequests.Commands.AddToQueue;

public sealed class AddReviewRequestToQueueCommandHandler(
    IReviewRequestRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<AddReviewRequestToQueueCommand, Unit>
{
    public async Task<Unit> Handle(
        AddReviewRequestToQueueCommand request,
        CancellationToken cancellationToken)
    {
        var reviewRequest = await repository.GetByIdAsync(request.ReviewRequestId, cancellationToken)
            ?? throw new NotFoundException($"Запрос на проверку '{request.ReviewRequestId}' не найден.");

        reviewRequest.Enqueue(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
