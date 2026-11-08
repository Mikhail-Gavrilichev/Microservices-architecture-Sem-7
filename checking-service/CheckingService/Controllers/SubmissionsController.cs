using CheckingService.Data;
using CheckingService.Models;
using CheckingService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CheckingService.Controllers;

[ApiController]
[Route("api/submissions")]
public class SubmissionsController(AppDbContext db, CheckingWorkflow workflow) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(SubmitRequest request)
    {
        var submission = await workflow.Submit(request);
        return CreatedAtAction(nameof(Get), new { id = submission.Id }, CheckingWorkflow.SubmissionView(submission));
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var list = await db.Submissions.AsNoTracking().OrderByDescending(s => s.Id).ToListAsync();
        return Ok(list.Select(CheckingWorkflow.SubmissionView));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var submission = await db.Submissions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        return submission == null ? NotFound(new { error = "Решение не найдено." }) : Ok(CheckingWorkflow.SubmissionView(submission));
    }

    [HttpPost("{id:int}/photo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(9_000_000)]
    public async Task<IActionResult> Photo(int id, [FromForm] UploadPhotoRequest request)
        => Ok(CheckingWorkflow.SubmissionView(await workflow.UploadPhoto(id, request)));

    [HttpPost("{id:int}/validate-photo")]
    public async Task<IActionResult> ValidatePhoto(int id, ValidatePhotoRequest request)
        => Ok(CheckingWorkflow.SubmissionView(await workflow.ValidatePhoto(id, request)));

    [HttpPost("{id:int}/resubmit")]
    public async Task<IActionResult> Resubmit(int id, ResubmitRequest request)
        => Ok(CheckingWorkflow.SubmissionView(await workflow.Resubmit(id, request)));
}

[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    [HttpGet("forms")]
    public IActionResult Forms() => Ok(TaskCatalog.Items.Select(x => new { x.GameId, x.FormId, x.TaskId, x.Condition }));
}
