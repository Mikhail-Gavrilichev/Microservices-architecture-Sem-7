using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskService.Infrastructure.Persistence;

/// <summary>
/// Используется только командой dotnet ef migrations. Не содержит production-секретов.
/// </summary>
public sealed class TaskDbContextFactory : IDesignTimeDbContextFactory<TaskDbContext>
{
    public TaskDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5432;Database=tasks;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsAssembly(typeof(TaskDbContext).Assembly.FullName))
            .Options;

        return new TaskDbContext(options);
    }
}
