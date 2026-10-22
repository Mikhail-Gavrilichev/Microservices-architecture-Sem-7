using Microsoft.EntityFrameworkCore;
using TaskService.Domain.Common;
using TaskService.Domain.GameTasks;

namespace TaskService.Infrastructure.Persistence;

public sealed class TaskDbContext(DbContextOptions<TaskDbContext> options) : DbContext(options)
{
    public DbSet<GameTask> Tasks => Set<GameTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaskDbContext).Assembly);
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
