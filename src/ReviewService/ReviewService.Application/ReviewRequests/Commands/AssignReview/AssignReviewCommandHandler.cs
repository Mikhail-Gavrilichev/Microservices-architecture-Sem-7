using MediatR;
using ReviewService.Application.Common.Exceptions;
using ReviewService.Application.Common.Interfaces;

namespace ReviewService.Application.ReviewRequests.Commands.AssignReview;

public sealed class AssignReviewCommandHandler(
    IReviewRequestRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<AssignReviewCommand, Unit>
{
    public async Task<Unit> Handle(AssignReviewCommand request, CancellationToken cancellationToken)
    {
        var reviewRequest = await repository.GetByIdAsync(request.ReviewRequestId, cancellationToken)
            ?? throw new NotFoundException($"Запрос на проверку '{request.ReviewRequestId}' не найден.");

        reviewRequest.Assign(request.JuryMemberId, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
