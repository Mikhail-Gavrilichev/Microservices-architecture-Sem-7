using System.ComponentModel.DataAnnotations;

namespace GameFormsService.Models;

public record CreateGameRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Name,
    string? Description);

public record TextRequest([Required] string Value);

public record CreateTaskRequest(
    [Range(1, int.MaxValue)] int GameId,
    [Required, StringLength(200, MinimumLength = 1)] string Title,
    string? Condition,
    string? CorrectAnswer,
    [Range(1, 10000)] int Points = 1);

public record EditTaskRequest(
    [Required, StringLength(200, MinimumLength = 1)] string Title,
    string? Condition,
    [Range(1, 10000)] int Points);

public record GenerateFormSetsRequest(
    [Range(1, int.MaxValue)] int GameId,
    [Required, MinLength(1)] List<string> TeamIds);

public record CreateFormRequest(
    [Range(1, int.MaxValue)] int FormSetId,
    [Range(1, int.MaxValue)] int TaskId);
