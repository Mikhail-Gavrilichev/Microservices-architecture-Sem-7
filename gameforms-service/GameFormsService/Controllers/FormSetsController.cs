using GameFormsService.Data;
using GameFormsService.Models;
using GameFormsService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameFormsService.Controllers;

[ApiController]
[Route("api/formsets")]
public class FormSetsController(AppDbContext db, WorkflowService workflow) : ControllerBase
{
    [HttpPost("generate")]
    public async Task<IActionResult> Generate(GenerateFormSetsRequest request)
        => StatusCode(201, await workflow.GenerateSets(request));

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] int? gameId)
    {
        var query = db.FormSets.AsNoTracking().AsQueryable();
        if (gameId.HasValue) query = query.Where(x => x.GameId == gameId.Value);
        return Ok(await query.OrderByDescending(x => x.Id).ToListAsync());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var set = await db.FormSets.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        if (set is null) return NotFound(new { error = "Набор форм не найден." });
        var forms = await db.Forms.AsNoTracking().Where(x => x.FormSetId == id)
            .Select(x => new { x.Id, x.FormSetId, x.TaskId, TaskTitle = x.Task.Title, x.Status, x.CreatedAt })
            .OrderBy(x => x.Id).ToListAsync();
        return Ok(new { set.Id, set.GameId, set.TeamId, set.Status, set.CreatedAt, Forms = forms });
    }

    [HttpPost("{id:int}/prepare")]
    public async Task<IActionResult> Prepare(int id) => Ok(await workflow.PrepareSet(id));

    [HttpPost("{id:int}/print")]
    public async Task<IActionResult> Print(int id) => Ok(await workflow.PrintSet(id));

    [HttpPost("{id:int}/issue")]
    public async Task<IActionResult> Issue(int id) => Ok(await workflow.IssueSet(id));
}
