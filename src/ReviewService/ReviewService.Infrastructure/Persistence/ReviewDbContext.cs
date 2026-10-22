using Microsoft.EntityFrameworkCore;
using ReviewService.Domain.Common;
using ReviewService.Domain.ReviewRequests;

namespace ReviewService.Infrastructure.Persistence;

public sealed class ReviewDbContext(DbContextOptions<ReviewDbContext> options) : DbContext(options)
{
    public DbSet<ReviewRequest> ReviewRequests => Set<ReviewRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReviewDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregatesWithEvents = ChangeTracker
            .Entries()
            .Where(entry => entry.Entity is AggregateRoot root && root.DomainEvents.Count > 0)
            .Select(entry => (AggregateRoot)entry.Entity)
            .ToArray();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var aggregate in aggregatesWithEvents)
        {
            aggregate.ClearDomainEvents();
        }

        return result;
    }
}
