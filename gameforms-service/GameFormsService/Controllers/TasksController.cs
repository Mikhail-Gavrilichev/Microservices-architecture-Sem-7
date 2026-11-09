using GameFormsService.Data;
using GameFormsService.Models;
using GameFormsService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameFormsService.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController(AppDbContext db, WorkflowService workflow) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateTaskRequest request)
    {
        var task = await workflow.CreateTask(request);
        return CreatedAtAction(nameof(Get), new { id = task.Id }, task);
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? gameId)
    {
        var query = db.Tasks.AsNoTracking().AsQueryable();
        if (gameId.HasValue) query = query.Where(x => x.GameId == gameId.Value);
        return Ok(await query.OrderByDescending(x => x.Id).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var task = await db.Tasks.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return task is null ? NotFound(new { error = "Задача не найдена." }) : Ok(task);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Edit(int id, EditTaskRequest request) => Ok(await workflow.EditTask(id, request));

    [HttpPut("{id:int}/answer")]
    public async Task<IActionResult> Answer(int id, TextRequest request) => Ok(await workflow.ChangeAnswer(id, request.Value));

    [HttpPost("{id:int}/publish")]
    public async Task<IActionResult> Publish(int id) => Ok(await workflow.PublishTask(id));

    [HttpPost("{id:int}/unpublish")]
    public async Task<IActionResult> Unpublish(int id) => Ok(await workflow.UnpublishTask(id));
}
