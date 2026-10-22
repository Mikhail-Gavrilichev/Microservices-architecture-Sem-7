using MediatR;

namespace TaskService.Application.GameTasks.Commands.ChangeTaskStatement;

/// <summary>Команда «Изменить условие».</summary>
public sealed record ChangeTaskStatementCommand(Guid TaskId, string Statement) : IRequest<Unit>;
