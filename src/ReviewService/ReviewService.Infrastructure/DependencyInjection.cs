using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ReviewService.Application.Common.Interfaces;
using ReviewService.Infrastructure.External;
using ReviewService.Infrastructure.Persistence;
using ReviewService.Infrastructure.Persistence.Repositories;

namespace ReviewService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException(
                "Строка подключения 'Postgres' не задана. Укажите ConnectionStrings__Postgres.");

        services.AddDbContext<ReviewDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(ReviewDbContext).Assembly.FullName)));

        services.AddScoped<IReviewRequestRepository, ReviewRequestRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        var taskServiceBaseUrl = configuration["TaskService:BaseUrl"];
        if (string.IsNullOrWhiteSpace(taskServiceBaseUrl))
        {
            throw new InvalidOperationException(
                "Адрес TaskService не задан. Укажите TaskService__BaseUrl.");
        }

        services
            .AddHttpClient<ITaskCatalog, TaskCatalogClient>(client =>
            {
                client.BaseAddress = new Uri(taskServiceBaseUrl.TrimEnd('/') + "/");
                client.Timeout = TimeSpan.FromSeconds(10);
            })
            .AddStandardResilienceHandler();

        return services;
    }
}
