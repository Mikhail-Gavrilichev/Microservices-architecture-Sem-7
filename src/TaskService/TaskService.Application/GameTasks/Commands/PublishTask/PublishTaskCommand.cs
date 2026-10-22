using MediatR;

namespace TaskService.Application.GameTasks.Commands.PublishTask;

/// <summary>Команда «Опубликовать задачу».</summary>
public sealed record PublishTaskCommand(Guid TaskId) : IRequest<Unit>;
