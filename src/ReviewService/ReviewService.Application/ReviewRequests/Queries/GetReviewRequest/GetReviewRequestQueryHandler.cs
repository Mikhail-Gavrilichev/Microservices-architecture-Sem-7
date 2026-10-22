using MediatR;
using ReviewService.Application.Common.Exceptions;
using ReviewService.Application.Common.Interfaces;
using ReviewService.Application.ReviewRequests.Dtos;

namespace ReviewService.Application.ReviewRequests.Queries.GetReviewRequest;

public sealed class GetReviewRequestQueryHandler(IReviewRequestRepository repository)
    : IRequestHandler<GetReviewRequestQuery, ReviewRequestDetailsDto>
{
    public async Task<ReviewRequestDetailsDto> Handle(
        GetReviewRequestQuery request,
        CancellationToken cancellationToken)
    {
        var reviewRequest = await repository.GetByIdAsync(request.ReviewRequestId, cancellationToken)
            ?? throw new NotFoundException($"Запрос на проверку '{request.ReviewRequestId}' не найден.");

        return ReviewRequestDetailsDto.FromDomain(reviewRequest);
    }
}
