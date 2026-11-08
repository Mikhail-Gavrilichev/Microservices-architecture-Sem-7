using CheckingService.Data;
using CheckingService.Models;
using CheckingService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CheckingService.Controllers;

[ApiController]
[Route("api/checks")]
public class ChecksController(AppDbContext db, CheckingWorkflow workflow) : ControllerBase
{
    private async Task<IActionResult> ViewCheck(int id)
    {
        var check = await db.CheckRequests.AsNoTracking().Include(x => x.Submission).FirstOrDefaultAsync(x => x.Id == id);
        return check == null ? NotFound(new { error = "Запрос на проверку не найден." }) : Ok(CheckingWorkflow.JudgeView(check));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateCheckRequest request)
    {
        var check = await workflow.CreateCheck(request.SubmissionId);
        return await ViewCheck(check.Id);
    }

    [HttpGet("queue")]
    public async Task<IActionResult> Queue()
    {
        var checks = await db.CheckRequests.AsNoTracking().Include(x => x.Submission)
            .Where(x => x.Status == CheckStatus.Queued || (x.Status == CheckStatus.Escalated && x.JudgeId == null))
            .OrderBy(x => x.CreatedAt).ToListAsync();
        return Ok(checks.Select(CheckingWorkflow.JudgeView));
    }

    [HttpGet("{id:int}")]
    public Task<IActionResult> Get(int id) => ViewCheck(id);

    [HttpGet("{id:int}/photo")]
    public async Task<IActionResult> Photo(int id)
    {
        var path = await workflow.PhotoPathForCheck(id);
        return PhysicalFile(path, Path.GetExtension(path).ToLowerInvariant() == ".png" ? "image/png" : "image/jpeg");
    }

    [HttpPost("{id:int}/assign")]
    public async Task<IActionResult> Assign(int id, AssignRequest request)
    {
        await workflow.Assign(id, request.JudgeId!.Value);
        return await ViewCheck(id);
    }

    [HttpPost("{id:int}/accept")]
    public async Task<IActionResult> Accept(int id, JudgeActionRequest request)
    {
        await workflow.Accept(id, request.JudgeId);
        return await ViewCheck(id);
    }

    [HttpPost("{id:int}/decline")]
    public async Task<IActionResult> Decline(int id, JudgeActionRequest request)
    {
        await workflow.Decline(id, request.JudgeId);
        return await ViewCheck(id);
    }

    [HttpPost("{id:int}/escalate")]
    public async Task<IActionResult> Escalate(int id, [FromQuery] int? seniorId)
    {
        await workflow.Escalate(id, seniorId);
        return await ViewCheck(id);
    }

    [HttpPost("{id:int}/complete")]
    public async Task<IActionResult> Complete(int id, CompleteRequest request)
    {
        var result = await workflow.Complete(id, request);
        return Ok(new { result.Id, result.CheckRequestId, result.IsCorrect, result.PointsAwarded, result.Comment, result.CreatedAt });
    }
}
