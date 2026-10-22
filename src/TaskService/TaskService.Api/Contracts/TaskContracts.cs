using TaskService.Application.Common.Models;
using TaskService.Application.GameTasks.Dtos;

namespace TaskService.Api.Contracts;

public sealed record CreateTaskRequest(
    string Title,
    string? Statement = null,
    string? CorrectAnswer = null);

public sealed record ChangeTaskStatementRequest(string Statement);

public sealed record ChangeTaskCorrectAnswerRequest(string CorrectAnswer);

