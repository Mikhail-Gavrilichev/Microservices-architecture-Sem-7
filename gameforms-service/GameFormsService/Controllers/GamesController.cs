using GameFormsService.Data;
using GameFormsService.Models;
using GameFormsService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GameFormsService.Controllers;

[ApiController]
[Route("api/games")]
public class GamesController(AppDbContext db, WorkflowService workflow) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateGameRequest request)
    {
        var game = await workflow.CreateGame(request);
        return CreatedAtAction(nameof(Get), new { id = game.Id }, game);
    }

    [HttpGet]
    public async Task<IActionResult> List() => Ok(await db.Games.AsNoTracking().OrderByDescending(x => x.Id).ToListAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var game = await db.Games.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
        return game is null ? NotFound(new { error = "Игра не найдена." }) : Ok(game);
    }

    [HttpPut("{id:int}/rules")]
    public async Task<IActionResult> Rules(int id, TextRequest request) => Ok(await workflow.UpdateGameText(id, request.Value, false));

    [HttpPut("{id:int}/form-rules")]
    public async Task<IActionResult> FormRules(int id, TextRequest request) => Ok(await workflow.UpdateGameText(id, request.Value, true));

    [HttpPost("{id:int}/publish")]
    public async Task<IActionResult> Publish(int id) => Ok(await workflow.PublishGame(id));
}
