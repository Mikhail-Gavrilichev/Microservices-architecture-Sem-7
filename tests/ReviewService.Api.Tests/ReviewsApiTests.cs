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
using ReviewService.Application.Common.Exceptions;
using ReviewService.Application.Common.Interfaces;
using ReviewService.Application.Common.Models;
using ReviewService.Application.ReviewRequests.Dtos;
using ReviewService.Domain.ReviewRequests;
using ReviewService.Infrastructure.Persistence;

namespace ReviewService.Api.Tests;

public sealed class ReviewsApiTests
{
    private static readonly Guid TaskId = Guid.NewGuid();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Fact]
    public async Task Review_can_be_queued_assigned_and_completed_without_exposing_team_to_jury()
    {
        using var factory = new ReviewApiFactory();
        using var client = factory.CreateClient();
        var juryMemberId = Guid.NewGuid();

        var createResponse = await client.PostAsJsonAsync("/api/review-requests", new
        {
            solutionId = Guid.NewGuid(),
            taskId = TaskId,
            participantAnswer = "Сидней",
            teamId = Guid.NewGuid()
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<ReviewRequestDetailsDto>(JsonOptions);
        Assert.NotNull(created);
        Assert.Equal(ReviewStatus.Created, created.Status);
        Assert.Equal("Условие задачи", created.TaskStatement);
        Assert.Equal("Правильный ответ", created.CorrectAnswer);

        var queueResponse = await client.PostAsync($"/api/review-requests/{created.Id}/queue", content: null);
        Assert.Equal(HttpStatusCode.NoContent, queueResponse.StatusCode);

        var assignResponse = await client.PostAsJsonAsync(
            $"/api/reviews/{created.Id}/assign",
            new { juryMemberId });
        Assert.Equal(HttpStatusCode.NoContent, assignResponse.StatusCode);

        var assignedResponse = await client.GetAsync($"/api/reviews/assigned/{juryMemberId}");
        Assert.Equal(HttpStatusCode.OK, assignedResponse.StatusCode);

        var assignedJson = await assignedResponse.Content.ReadAsStringAsync();
        Assert.DoesNotContain("teamId", assignedJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("solutionId", assignedJson, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("correctAnswer", assignedJson, StringComparison.OrdinalIgnoreCase);

        var completeResponse = await client.PostAsJsonAsync(
            $"/api/reviews/{created.Id}/complete",
            new { juryMemberId, verdict = "Incorrect", comment = "Ответ неверный" });
        Assert.Equal(HttpStatusCode.NoContent, completeResponse.StatusCode);

        var completed = await client.GetFromJsonAsync<IReadOnlyList<JuryReviewDto>>(
            $"/api/reviews/assigned/{juryMemberId}",
            JsonOptions);

        var review = Assert.Single(completed!);
        Assert.Equal(ReviewStatus.Completed, review.Status);
        Assert.Equal(ReviewVerdict.Incorrect, review.Verdict);
    }

    [Fact]
    public async Task Jury_cannot_read_review_of_another_member()
    {
        using var factory = new ReviewApiFactory();
        using var client = factory.CreateClient();
        var juryMemberId = Guid.NewGuid();

        var created = await CreateQueuedAndAssignedReview(client, juryMemberId);

        var response = await client.GetAsync($"/api/reviews/{created}/jury/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Empty_participant_answer_returns_validation_problem()
    {
        using var factory = new ReviewApiFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/review-requests", new
        {
            solutionId = Guid.NewGuid(),
            taskId = TaskId,
            participantAnswer = "",
            teamId = Guid.NewGuid()
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Ошибка валидации", body);
    }

    private static async Task<Guid> CreateQueuedAndAssignedReview(HttpClient client, Guid juryMemberId)
    {
        var createResponse = await client.PostAsJsonAsync("/api/review-requests", new
        {
            solutionId = Guid.NewGuid(),
            taskId = TaskId,
            participantAnswer = "Ответ",
            teamId = Guid.NewGuid()
        });

        var created = await createResponse.Content.ReadFromJsonAsync<ReviewRequestDetailsDto>(JsonOptions);
        Assert.NotNull(created);

        await client.PostAsync($"/api/review-requests/{created.Id}/queue", content: null);
        await client.PostAsJsonAsync($"/api/reviews/{created.Id}/assign", new { juryMemberId });

        return created.Id;
    }

    private sealed class ReviewApiFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:ApplyMigrations"] = "false",
                    ["ConnectionStrings:Postgres"] = "Host=localhost;Database=ignored;Username=test;Password=test",
                    ["TaskService:BaseUrl"] = "http://localhost:5101"
                });
            });

            builder.ConfigureServices(services =>
            {
                services.RemoveAll<DbContextOptions<ReviewDbContext>>();
                services.RemoveAll<IDbContextOptionsConfiguration<ReviewDbContext>>();
                services.RemoveAll<ReviewDbContext>();
                var databaseName = $"reviews-api-{Guid.NewGuid()}";
                services.AddDbContext<ReviewDbContext>(options =>
                    options.UseInMemoryDatabase(databaseName));

                services.RemoveAll<ITaskCatalog>();
                services.AddSingleton<ITaskCatalog>(new FakeTaskCatalog(TaskId));
            });
        }
    }

    private sealed class FakeTaskCatalog(Guid taskId) : ITaskCatalog
    {
        public Task<TaskSnapshot> GetPublishedTaskAsync(Guid requestedTaskId, CancellationToken cancellationToken)
        {
            if (requestedTaskId != taskId)
            {
                throw new NotFoundException($"Задача '{requestedTaskId}' не найдена.");
            }

            return Task.FromResult(new TaskSnapshot(
                taskId,
                "Условие задачи",
                "Правильный ответ",
                DateTimeOffset.UtcNow));
        }
    }
}
