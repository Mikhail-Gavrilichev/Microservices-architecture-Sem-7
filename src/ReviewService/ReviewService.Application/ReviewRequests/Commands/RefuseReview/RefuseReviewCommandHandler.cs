using MediatR;
using ReviewService.Application.Common.Exceptions;
using ReviewService.Application.Common.Interfaces;

namespace ReviewService.Application.ReviewRequests.Commands.RefuseReview;

public sealed class RefuseReviewCommandHandler(
    IReviewRequestRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<RefuseReviewCommand, Unit>
{
    public async Task<Unit> Handle(RefuseReviewCommand request, CancellationToken cancellationToken)
    {
        var reviewRequest = await repository.GetByIdAsync(request.ReviewRequestId, cancellationToken)
            ?? throw new NotFoundException($"Запрос на проверку '{request.ReviewRequestId}' не найден.");

        reviewRequest.Refuse(request.JuryMemberId, request.Reason, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
