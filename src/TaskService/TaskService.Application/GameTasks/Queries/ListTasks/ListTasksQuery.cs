using MediatR;
using TaskService.Application.Common.Models;
using TaskService.Application.GameTasks.Dtos;
using TaskService.Domain.GameTasks;

namespace TaskService.Application.GameTasks.Queries.ListTasks;

public sealed record ListTasksQuery(
    GameTaskStatus? Status = null,
    string? Search = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<TaskDto>>;
