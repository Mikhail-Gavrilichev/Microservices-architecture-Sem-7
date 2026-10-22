using MediatR;
using ReviewService.Application.Common.Exceptions;
using ReviewService.Application.Common.Interfaces;
using ReviewService.Application.ReviewRequests.Dtos;

namespace ReviewService.Application.ReviewRequests.Queries.GetJuryReview;

public sealed class GetJuryReviewQueryHandler(IReviewRequestRepository repository)
    : IRequestHandler<GetJuryReviewQuery, JuryReviewDto>
{
    public async Task<JuryReviewDto> Handle(
        GetJuryReviewQuery request,
        CancellationToken cancellationToken)
    {
        var reviewRequest = await repository.GetByIdAsync(request.ReviewRequestId, cancellationToken);

        if (reviewRequest is null || reviewRequest.AssignedJuryMemberId != request.JuryMemberId)
        {
            throw new NotFoundException($"Проверка '{request.ReviewRequestId}' не найдена.");
        }

        return JuryReviewDto.FromDomain(reviewRequest);
    }
}
