using Microsoft.EntityFrameworkCore;
using TaskService.Infrastructure.Persistence;

namespace TaskService.Api.Extensions;

public static class DatabaseExtensions
{
    /// <summary>
    /// Применяет миграции при старте. Повторяет попытки, пока PostgreSQL поднимается.
    /// </summary>
    public static async Task ApplyMigrationsAsync(
        this WebApplication app,
        CancellationToken cancellationToken = default)
    {
        const int maxAttempts = 10;
        var logger = app.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("DatabaseMigration");

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                await using var scope = app.Services.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<TaskDbContext>();
                await dbContext.Database.MigrateAsync(cancellationToken);
                logger.LogInformation("Миграции TaskService применены.");
                return;
            }
            catch (Exception exception) when (attempt < maxAttempts)
            {
                logger.LogWarning(
                    exception,
                    "Не удалось применить миграции TaskService (попытка {Attempt}/{MaxAttempts}). Повтор через 3 с.",
                    attempt,
                    maxAttempts);

                await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);
            }
        }
    }
}
