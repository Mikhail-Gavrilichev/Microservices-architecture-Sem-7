using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ReviewService.Infrastructure.Persistence;

/// <summary>
/// Используется только командой dotnet ef migrations. Не содержит production-секретов.
/// </summary>
public sealed class ReviewDbContextFactory : IDesignTimeDbContextFactory<ReviewDbContext>
{
    public ReviewDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ReviewDbContext>()
            .UseNpgsql(
                "Host=localhost;Port=5433;Database=reviews;Username=postgres;Password=postgres",
                npgsql => npgsql.MigrationsAssembly(typeof(ReviewDbContext).Assembly.FullName))
            .Options;

        return new ReviewDbContext(options);
    }
}
