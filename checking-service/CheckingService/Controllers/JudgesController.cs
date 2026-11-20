using CheckingService.Data;
using CheckingService.Models;
using CheckingService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CheckingService.Controllers;

[ApiController]
[Route("api/judges")]
public class JudgesController(AppDbContext db, CheckingWorkflow workflow) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateJudgeRequest request)
        => StatusCode(201, await workflow.CreateJudge(request));

    [HttpGet]
    public async Task<IActionResult> List() => Ok(await db.Judges.AsNoTracking().OrderBy(x => x.Id).ToListAsync());

    [HttpGet("{id:int}/checks")]
    public async Task<IActionResult> Checks(int id)
    {
        if (!await db.Judges.AnyAsync(x => x.Id == id)) return NotFound(new { error = "Член жюри не найден." });
        var checks = await db.CheckRequests.AsNoTracking().Include(x => x.Submission)
            .Where(x => x.JudgeId == id).OrderByDescending(x => x.Id).ToListAsync();
        return Ok(checks.Select(CheckingWorkflow.JudgeView));
    }
}
