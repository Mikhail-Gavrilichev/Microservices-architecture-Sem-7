using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskService.Application.GameTasks.Dtos;
using TaskService.Infrastructure.Persistence;

namespace TaskService.Api.Tests;

public sealed class TasksApiTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Task_can_be_created_published_and_read_as_snapshot()
    {
        using var factory = new TaskApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Задача 1",
            statement = "Столица Австралии?",
            correctAnswer = "Канберра"
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal("Draft", created.Status.ToString());

        var publishResponse = await client.PostAsync($"/api/tasks/{created.Id}/publish", content: null);
        Assert.Equal(HttpStatusCode.NoContent, publishResponse.StatusCode);

        var snapshotResponse = await client.GetAsync($"/api/tasks/{created.Id}/snapshot");
        Assert.Equal(HttpStatusCode.OK, snapshotResponse.StatusCode);

        var snapshot = await snapshotResponse.Content.ReadFromJsonAsync<TaskSnapshotDto>();
        Assert.NotNull(snapshot);
        Assert.Equal("Столица Австралии?", snapshot.Statement);
        Assert.Equal("Канберра", snapshot.CorrectAnswer);
    }

    [Fact]
    public async Task Draft_task_has_no_snapshot()
    {
        using var factory = new TaskApiFactory();
        using var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync("/api/tasks", new
        {
            title = "Черновик",
            statement = "Условие",
            correctAnswer = "Ответ"
        });

        var created = await createResponse.Content.ReadFromJsonAsync<TaskDto>(JsonOptions);
        Assert.NotNull(created);

        var snapshotResponse = await client.GetAsync($"/api/tasks/{created.Id}/snapshot");

        Assert.Equal(HttpStatusCode.Conflict, snapshotResponse.StatusCode);
    }

    [Fact]
    public async Task Empty_title_returns_problem_details()
    {
        using var factory = new TaskApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tasks", new
        {
            title = "",
            statement = "Условие",
            correctAnswer = "Ответ"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Ошибка валидации", body);
    }

    private sealed class TaskApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:ApplyMigrations"] = "false",
                    ["ConnectionStrings:Postgres"] = "Host=localhost;Database=ignored;Username=test;Password=test"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<TaskDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<TaskDbContext>>();
                services.RemoveAll<TaskDbContext>();
                var databaseName = $"tasks-api-{Guid.NewGuid()}";
                services.AddDbContext<TaskDbContext>(options =>
                    options.UseInMemoryDatabase(databaseName));
            });
        }
    }
}
