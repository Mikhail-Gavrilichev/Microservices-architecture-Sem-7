using MediatR;
using Microsoft.AspNetCore.Mvc;
using TaskService.Api.Contracts;
using TaskService.Application.Common.Models;
using TaskService.Application.GameTasks.Commands.ChangeTaskCorrectAnswer;
using TaskService.Application.GameTasks.Commands.ChangeTaskStatement;
using TaskService.Application.GameTasks.Commands.CreateTask;
using TaskService.Application.GameTasks.Commands.PublishTask;
using TaskService.Application.GameTasks.Dtos;
using TaskService.Application.GameTasks.Queries.GetTask;
using TaskService.Application.GameTasks.Queries.GetTaskSnapshot;
using TaskService.Application.GameTasks.Queries.ListTasks;
using TaskService.Domain.GameTasks;

namespace TaskService.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public sealed class TasksController(ISender sender) : ControllerBase
{
    /// <summary>Создать задачу (команда «Создать задачу»).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskDto>> Create(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await sender.Send(
            new CreateTaskCommand(request.Title, request.Statement, request.CorrectAnswer),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    /// <summary>Получить задачу по идентификатору.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> GetById(Guid id, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetTaskQuery(id), cancellationToken));

    /// <summary>Список задач с фильтрацией и постраничной выдачей.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TaskDto>>> List(
        [FromQuery] GameTaskStatus? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new ListTasksQuery(status, search, page, pageSize), cancellationToken));

    /// <summary>Изменить условие задачи (только в статусе Draft).</summary>
    [HttpPut("{id:guid}/statement")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeStatement(
        Guid id,
        [FromBody] ChangeTaskStatementRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ChangeTaskStatementCommand(id, request.Statement), cancellationToken);
        return NoContent();
    }

    /// <summary>Изменить правильный ответ (только в статусе Draft).</summary>
    [HttpPut("{id:guid}/correct-answer")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ChangeCorrectAnswer(
        Guid id,
        [FromBody] ChangeTaskCorrectAnswerRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new ChangeTaskCorrectAnswerCommand(id, request.CorrectAnswer), cancellationToken);
        return NoContent();
    }

    /// <summary>Опубликовать задачу (команда «Опубликовать задачу»).</summary>
    [HttpPost("{id:guid}/publish")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Publish(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new PublishTaskCommand(id), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Внутренний контракт для ReviewService: снимок опубликованной задачи.
    /// Черновики намеренно недоступны.
    /// </summary>
    [HttpGet("{id:guid}/snapshot")]
    [ProducesResponseType(typeof(TaskSnapshotDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TaskSnapshotDto>> GetSnapshot(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetTaskSnapshotQuery(id), cancellationToken));
}
