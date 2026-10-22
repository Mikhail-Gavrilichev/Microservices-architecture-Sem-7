using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskService.Application.Common.Interfaces;
using TaskService.Infrastructure.Persistence;
using TaskService.Infrastructure.Persistence.Repositories;

namespace TaskService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Строка подключения 'Postgres' не задана. Укажите ConnectionStrings__Postgres.");

        services.AddDbContext<TaskDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(TaskDbContext).Assembly.FullName)));

        services.AddScoped<IGameTaskRepository, GameTaskRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
