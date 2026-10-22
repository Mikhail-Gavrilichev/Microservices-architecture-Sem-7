using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ReviewService.Application.Common.Exceptions;
using ReviewService.Application.Common.Interfaces;
using ReviewService.Application.Common.Models;

namespace ReviewService.Infrastructure.External;

/// <summary>
/// Антикоррупционный клиент TaskService.
/// ReviewService не использует БД TaskService и не делит с ним доменные модели.
/// </summary>
public sealed class TaskCatalogClient(
    HttpClient httpClient,
    ILogger<TaskCatalogClient> logger) : ITaskCatalog
{
    public async Task<TaskSnapshot> GetPublishedTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await httpClient.GetAsync(
                $"api/tasks/{taskId}/snapshot",
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                throw new NotFoundException($"Задача '{taskId}' не найдена в TaskService.");
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                throw new ConflictException($"Задача '{taskId}' ещё не опубликована в TaskService.");
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "TaskService вернул статус {StatusCode} для задачи {TaskId}.",
                    (int)response.StatusCode,
                    taskId);

                throw new ExternalServiceException(
                    $"TaskService вернул неожиданный статус {(int)response.StatusCode}.");
            }

            var snapshot = await response.Content.ReadFromJsonAsync<TaskSnapshotResponse>(
                cancellationToken: cancellationToken);

            if (snapshot is null ||
                snapshot.TaskId != taskId ||
                string.IsNullOrWhiteSpace(snapshot.Statement) ||
                string.IsNullOrWhiteSpace(snapshot.CorrectAnswer))
            {
                throw new ExternalServiceException(
                    $"TaskService вернул некорректный снимок задачи '{taskId}'.");
            }

            return new TaskSnapshot(
                snapshot.TaskId,
                snapshot.Statement,
                snapshot.CorrectAnswer,
                snapshot.PublishedAt);
        }
        catch (HttpRequestException exception)
        {
            throw new ExternalServiceException("Сервис задач недоступен.", exception);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExternalServiceException("Сервис задач не ответил вовремя.", exception);
        }
        catch (JsonException exception)
        {
            throw new ExternalServiceException("TaskService вернул некорректный JSON.", exception);
        }
    }

    private sealed record TaskSnapshotResponse(
        Guid TaskId,
        string Statement,
        string CorrectAnswer,
        DateTimeOffset PublishedAt);
}
