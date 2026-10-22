using Microsoft.EntityFrameworkCore;
using ReviewService.Application.Common.Interfaces;
using ReviewService.Domain.ReviewRequests;

namespace ReviewService.Infrastructure.Persistence.Repositories;

public sealed class ReviewRequestRepository(ReviewDbContext dbContext) : IReviewRequestRepository
{
    public Task<ReviewRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.ReviewRequests.FirstOrDefaultAsync(request => request.Id == id, cancellationToken);

    public async Task AddAsync(ReviewRequest request, CancellationToken cancellationToken) =>
        await dbContext.ReviewRequests.AddAsync(request, cancellationToken);

    public Task<ReviewRequest?> GetNextQueuedAsync(CancellationToken cancellationToken) =>
        dbContext.ReviewRequests
            .Where(request => request.Status == ReviewStatus.Queued)
            .OrderBy(request => request.QueuedAt)
            .ThenBy(request => request.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ReviewRequest>> GetQueueAsync(
        int limit,
        CancellationToken cancellationToken) =>
        await dbContext.ReviewRequests
            .AsNoTracking()
            .Where(request => request.Status == ReviewStatus.Queued)
            .OrderBy(request => request.QueuedAt)
            .ThenBy(request => request.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ReviewRequest>> GetAssignedToAsync(
        Guid juryMemberId,
        CancellationToken cancellationToken) =>
        await dbContext.ReviewRequests
            .AsNoTracking()
            .Where(request =>
                request.AssignedJuryMemberId == juryMemberId &&
                (request.Status == ReviewStatus.Assigned ||
                 request.Status == ReviewStatus.Accepted ||
                 request.Status == ReviewStatus.TransferredToSenior ||
                 request.Status == ReviewStatus.Completed))
            .OrderByDescending(request => request.AssignedAt)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<ReviewRequest> Items, int TotalCount)> ListAsync(
        ReviewStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.ReviewRequests.AsNoTracking().AsQueryable();

        if (status is not null)
        {
            query = query.Where(request => request.Status == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(request => request.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
