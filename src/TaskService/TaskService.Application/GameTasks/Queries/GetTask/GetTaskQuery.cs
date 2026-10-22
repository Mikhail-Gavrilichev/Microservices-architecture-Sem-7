using MediatR;
using TaskService.Application.GameTasks.Dtos;

namespace TaskService.Application.GameTasks.Queries.GetTask;

public sealed record GetTaskQuery(Guid TaskId) : IRequest<TaskDto>;
