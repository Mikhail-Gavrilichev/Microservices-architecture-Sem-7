using MediatR;
using ReviewService.Application.Common.Exceptions;
using ReviewService.Application.Common.Interfaces;

namespace ReviewService.Application.ReviewRequests.Commands.AcceptReview;

public sealed class AcceptReviewCommandHandler(
    IReviewRequestRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<AcceptReviewCommand, Unit>
{
    public async Task<Unit> Handle(AcceptReviewCommand request, CancellationToken cancellationToken)
    {
        var reviewRequest = await repository.GetByIdAsync(request.ReviewRequestId, cancellationToken)
            ?? throw new NotFoundException($"Запрос на проверку '{request.ReviewRequestId}' не найден.");

        reviewRequest.Accept(request.JuryMemberId, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
