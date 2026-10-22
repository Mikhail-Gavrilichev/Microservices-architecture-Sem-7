using MediatR;
using ReviewService.Application.Common.Interfaces;
using ReviewService.Application.ReviewRequests.Dtos;

namespace ReviewService.Application.ReviewRequests.Commands.AssignNextReview;

public sealed class AssignNextReviewCommandHandler(
    IReviewRequestRepository repository,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
    : IRequestHandler<AssignNextReviewCommand, JuryReviewDto?>
{
    public async Task<JuryReviewDto?> Handle(
        AssignNextReviewCommand request,
        CancellationToken cancellationToken)
    {
        var nextRequest = await repository.GetNextQueuedAsync(cancellationToken);
        if (nextRequest is null)
        {
            return null;
        }

        nextRequest.Assign(request.JuryMemberId, timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return JuryReviewDto.FromDomain(nextRequest);
    }
}
