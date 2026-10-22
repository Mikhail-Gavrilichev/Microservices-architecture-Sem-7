using MediatR;
using TaskService.Application.GameTasks.Dtos;

namespace TaskService.Application.GameTasks.Commands.CreateTask;

/// <summary>Команда «Создать задачу».</summary>
public sealed record CreateTaskCommand(
    string Title,
    string? Statement = null,
    string? CorrectAnswer = null) : IRequest<TaskDto>;
