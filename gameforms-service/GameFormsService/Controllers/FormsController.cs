using GameFormsService.Data;
using GameFormsService.Models;
using GameFormsService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameFormsService.Controllers;

[ApiController]
[Route("api/forms")]
public class FormsController(AppDbContext db, WorkflowService workflow) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateFormRequest request)
    {
        var form = await workflow.CreateForm(request);
        return CreatedAtAction(nameof(Get), new { id = form.Id }, new { form.Id, form.FormSetId, form.TaskId, form.Status, form.CreatedAt });
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? formSetId)
    {
        var query = db.Forms.AsNoTracking().AsQueryable();
        if (formSetId.HasValue) query = query.Where(x => x.FormSetId == formSetId.Value);
        return Ok(await query.OrderByDescending(x => x.Id)
            .Select(x => new { x.Id, x.FormSetId, x.TaskId, TaskTitle = x.Task.Title, x.Status, x.CreatedAt }).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var form = await db.Forms.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.Id, x.FormSetId, x.TaskId, TaskTitle = x.Task.Title, x.Status, x.CreatedAt }).FirstOrDefaultAsync();
        return form is null ? NotFound(new { error = "Форма не найдена." }) : Ok(form);
    }

    [HttpPost("{id:int}/prepare")]
    public async Task<IActionResult> Prepare(int id)
    {
        var form = await workflow.PrepareForm(id);
        return Ok(new { form.Id, form.FormSetId, form.TaskId, form.Status, form.CreatedAt });
    }

    [HttpPost("{id:int}/print")]
    public async Task<IActionResult> Print(int id)
    {
        var form = await workflow.PrintForm(id);
        return Ok(new { form.Id, form.FormSetId, form.TaskId, form.Status, form.CreatedAt });
    }
}
