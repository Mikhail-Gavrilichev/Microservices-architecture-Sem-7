using MediatR;
using ReviewService.Application.Common.Exceptions;
using ReviewService.Application.Common.Interfaces;

namespace ReviewService.Application.ReviewRequests.Commands.TransferReviewToSenior;

public sealed class TransferReviewToSeniorCommandHandler(
    IReviewRequestRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<TransferReviewToSeniorCommand, Unit>
{
    public async Task<Unit> Handle(
        TransferReviewToSeniorCommand request,
        CancellationToken cancellationToken)
    {
        var reviewRequest = await repository.GetByIdAsync(request.ReviewRequestId, cancellationToken)
            ?? throw new NotFoundException($"Запрос на проверку '{request.ReviewRequestId}' не найден.");

        reviewRequest.TransferToSenior(request.SeniorJuryMemberId, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
