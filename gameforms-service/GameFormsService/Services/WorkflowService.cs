using System.Data;
using GameFormsService.Data;
using GameFormsService.Models;
using Microsoft.EntityFrameworkCore;
using TaskStatus = GameFormsService.Models.TaskStatus;

namespace GameFormsService.Services;

public class BusinessException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public class WorkflowService(AppDbContext db)
{
    private static BusinessException Bad(string message) => new(400, message);
    private static BusinessException Conflict(string message) => new(409, message);
    private static BusinessException Missing(string what) => new(404, $"{what} не найден(а).");

    public async Task<Game> CreateGame(CreateGameRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name)) throw Bad("Название игры обязательно.");
        var game = new Game { Name = request.Name.Trim(), Description = request.Description?.Trim() ?? "" };
        db.Games.Add(game);
        await db.SaveChangesAsync();
        return game;
    }

    public async Task<Game> UpdateGameText(int id, string value, bool formRules)
    {
        var game = await db.Games.FindAsync(id) ?? throw Missing("Игра");
        if (game.Status != GameStatus.Draft) throw Conflict("Правила опубликованной игры менять нельзя.");
        if (formRules) game.FormRules = value.Trim(); else game.Rules = value.Trim();
        await db.SaveChangesAsync();
        return game;
    }

    public async Task<Game> PublishGame(int id)
    {
        var game = await db.Games.FindAsync(id) ?? throw Missing("Игра");
        if (string.IsNullOrWhiteSpace(game.Name)) throw Bad("Нельзя опубликовать игру без названия.");
        if (game.Status != GameStatus.Draft) throw Conflict("Игра уже опубликована.");
        game.Status = GameStatus.Published;
        await db.SaveChangesAsync();
        return game;
    }

    public async Task<GameTask> CreateTask(CreateTaskRequest request)
    {
        if (!await db.Games.AnyAsync(x => x.Id == request.GameId)) throw Missing("Игра");
        if (string.IsNullOrWhiteSpace(request.Title)) throw Bad("Название задачи обязательно.");
        var task = new GameTask
        {
            GameId = request.GameId, Title = request.Title.Trim(),
            Condition = request.Condition?.Trim() ?? "", CorrectAnswer = request.CorrectAnswer?.Trim() ?? "",
            Points = request.Points
        };
        db.Tasks.Add(task);
        await db.SaveChangesAsync();
        return task;
    }

    public async Task<GameTask> EditTask(int id, EditTaskRequest request)
    {
        var task = await db.Tasks.FindAsync(id) ?? throw Missing("Задача");
        if (task.Status != TaskStatus.Draft) throw Conflict("Для редактирования сначала снимите задачу с публикации.");
        if (string.IsNullOrWhiteSpace(request.Title)) throw Bad("Название задачи обязательно.");
        task.Title = request.Title.Trim();
        task.Condition = request.Condition?.Trim() ?? "";
        task.Points = request.Points;
        await db.SaveChangesAsync();
        return task;
    }

    public async Task<GameTask> ChangeAnswer(int id, string answer)
    {
        var task = await db.Tasks.FindAsync(id) ?? throw Missing("Задача");
        if (task.Status != TaskStatus.Draft) throw Conflict("Для изменения ответа сначала снимите задачу с публикации.");
        task.CorrectAnswer = answer.Trim();
        await db.SaveChangesAsync();
        return task;
    }

    public async Task<GameTask> PublishTask(int id)
    {
        var task = await db.Tasks.FindAsync(id) ?? throw Missing("Задача");
        if (task.Status != TaskStatus.Draft) throw Conflict("Задача уже опубликована.");
        if (string.IsNullOrWhiteSpace(task.Condition) || string.IsNullOrWhiteSpace(task.CorrectAnswer))
            throw Bad("Для публикации нужны условие и правильный ответ.");
        task.Status = TaskStatus.Published;
        await db.SaveChangesAsync();
        return task;
    }

    public async Task<GameTask> UnpublishTask(int id)
    {
        var task = await db.Tasks.FindAsync(id) ?? throw Missing("Задача");
        if (task.Status != TaskStatus.Published) throw Conflict("Задача уже в черновике.");
        if (await db.Forms.AnyAsync(x => x.TaskId == id)) throw Conflict("Задача назначена форме; снять с публикации нельзя.");
        task.Status = TaskStatus.Draft;
        await db.SaveChangesAsync();
        return task;
    }

    public async Task<List<FormSet>> GenerateSets(GenerateFormSetsRequest request)
    {
        var teams = request.TeamIds.Select(x => x?.Trim() ?? "").ToList();
        if (teams.Count == 0 || teams.Any(x => x.Length == 0 || x.Length > 100))
            throw Bad("Укажите хотя бы один TeamId длиной до 100 символов.");
        if (teams.Distinct(StringComparer.OrdinalIgnoreCase).Count() != teams.Count)
            throw Bad("TeamId не должны повторяться.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var game = await db.Games.FindAsync(request.GameId) ?? throw Missing("Игра");
        if (game.Status != GameStatus.Published) throw Conflict("Наборы создаются только для опубликованной игры.");
        if (await db.FormSets.AnyAsync(x => x.GameId == request.GameId))
            throw Conflict("Для игры уже созданы наборы. Список команд фиксируется одной генерацией.");
        var sets = teams.Select(x => new FormSet { GameId = request.GameId, TeamId = x }).ToList();
        db.FormSets.AddRange(sets);
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return sets;
    }

    public async Task<Form> CreateForm(CreateFormRequest request)
    {
        var set = await db.FormSets.FindAsync(request.FormSetId) ?? throw Missing("Набор форм");
        var task = await db.Tasks.FindAsync(request.TaskId) ?? throw Missing("Задача");
        if (set.Status != FormSetStatus.Created) throw Conflict("Добавлять формы можно только в новый набор.");
        if (task.GameId != set.GameId) throw Bad("Задача должна принадлежать той же игре, что и набор.");
        if (task.Status != TaskStatus.Published) throw Conflict("Нельзя назначить неопубликованную задачу.");
        if (await db.Forms.AnyAsync(x => x.FormSetId == set.Id && x.TaskId == task.Id))
            throw Conflict("Эта задача уже назначена набору.");
        var form = new Form { FormSetId = set.Id, TaskId = task.Id };
        db.Forms.Add(form);
        await db.SaveChangesAsync();
        return form;
    }

    public async Task<Form> PrepareForm(int id)
    {
        var form = await db.Forms.FindAsync(id) ?? throw Missing("Форма");
        var set = await db.FormSets.FindAsync(form.FormSetId) ?? throw Missing("Набор форм");
        if (form.Status != FormStatus.Created || set.Status != FormSetStatus.Created)
            throw Conflict("Подготовить можно только новую форму в новом наборе.");
        form.Status = FormStatus.Prepared;
        await db.SaveChangesAsync();
        return form;
    }

    public async Task<Form> PrintForm(int id)
    {
        var form = await db.Forms.FindAsync(id) ?? throw Missing("Форма");
        var set = await db.FormSets.FindAsync(form.FormSetId) ?? throw Missing("Набор форм");
        if (form.Status != FormStatus.Prepared || set.Status != FormSetStatus.Prepared)
            throw Conflict("Перед печатью подготовьте форму и набор.");
        form.Status = FormStatus.Printed;
        await db.SaveChangesAsync();
        return form;
    }

    public async Task<FormSet> PrepareSet(int id)
    {
        var set = await db.FormSets.FindAsync(id) ?? throw Missing("Набор форм");
        if (set.Status != FormSetStatus.Created) throw Conflict("Подготовить можно только новый набор.");
        var forms = await db.Forms.Where(x => x.FormSetId == id).ToListAsync();
        if (forms.Count == 0) throw Conflict("Сначала добавьте хотя бы одну форму с задачей.");
        foreach (var form in forms) form.Status = FormStatus.Prepared;
        set.Status = FormSetStatus.Prepared;
        await db.SaveChangesAsync();
        return set;
    }

    public async Task<FormSet> PrintSet(int id)
    {
        var set = await db.FormSets.FindAsync(id) ?? throw Missing("Набор форм");
        if (set.Status != FormSetStatus.Prepared) throw Conflict("Перед печатью подготовьте набор.");
        var forms = await db.Forms.Where(x => x.FormSetId == id).ToListAsync();
        if (forms.Count == 0 || forms.Any(x => x.Status == FormStatus.Created))
            throw Conflict("Перед печатью все формы должны быть подготовлены.");
        foreach (var form in forms) form.Status = FormStatus.Printed;
        set.Status = FormSetStatus.Printed;
        await db.SaveChangesAsync();
        return set;
    }

    public async Task<FormSet> IssueSet(int id)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var set = await db.FormSets.FindAsync(id) ?? throw Missing("Набор форм");
        if (set.Status != FormSetStatus.Printed) throw Conflict("Выдать можно только напечатанный набор; повторная выдача запрещена.");
        var registered = await db.FormSets.CountAsync(x => x.GameId == set.GameId);
        var issued = await db.FormSets.CountAsync(x => x.GameId == set.GameId && x.Status == FormSetStatus.Issued);
        if (issued >= registered) throw Conflict("Число выданных наборов не может превышать число зарегистрированных команд.");
        set.Status = FormSetStatus.Issued;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
        return set;
    }
}
