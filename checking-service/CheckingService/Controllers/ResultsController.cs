using CheckingService.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CheckingService.Controllers;

[ApiController]
[Route("api/results")]
public class ResultsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List() => Ok(await db.CheckResults.AsNoTracking()
        .OrderByDescending(x => x.Id)
        .Select(x => new { x.Id, x.CheckRequestId, x.CheckRequest.SubmissionId, x.IsCorrect,
            x.PointsAwarded, x.Comment, x.CreatedAt }).ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var result = await db.CheckResults.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new { x.Id, x.CheckRequestId, x.CheckRequest.SubmissionId, x.IsCorrect,
                x.PointsAwarded, x.Comment, x.CreatedAt }).FirstOrDefaultAsync();
        return result == null ? NotFound(new { error = "Результат не найден." }) : Ok(result);
    }
}
