using ReviewService.Domain.ReviewRequests;

namespace ReviewService.Application.Common.Interfaces;

public interface IReviewRequestRepository
{
    Task<ReviewRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task AddAsync(ReviewRequest request, CancellationToken cancellationToken);

    Task<ReviewRequest?> GetNextQueuedAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ReviewRequest>> GetQueueAsync(int limit, CancellationToken cancellationToken);

    Task<IReadOnlyList<ReviewRequest>> GetAssignedToAsync(
        Guid juryMemberId,
        CancellationToken cancellationToken);

    Task<(IReadOnlyList<ReviewRequest> Items, int TotalCount)> ListAsync(
        ReviewStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);
}
