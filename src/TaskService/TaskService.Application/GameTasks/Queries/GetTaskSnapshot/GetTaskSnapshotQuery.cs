using MediatR;
using TaskService.Application.GameTasks.Dtos;

namespace TaskService.Application.GameTasks.Queries.GetTaskSnapshot;

/// <summary>
/// Запрос снимка опубликованной задачи для ReviewService.
/// </summary>
public sealed record GetTaskSnapshotQuery(Guid TaskId) : IRequest<TaskSnapshotDto>;
