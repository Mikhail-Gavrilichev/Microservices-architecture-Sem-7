using MediatR;

namespace TaskService.Application.GameTasks.Commands.ChangeTaskCorrectAnswer;

/// <summary>Команда «Изменить правильный ответ».</summary>
public sealed record ChangeTaskCorrectAnswerCommand(Guid TaskId, string CorrectAnswer) : IRequest<Unit>;
