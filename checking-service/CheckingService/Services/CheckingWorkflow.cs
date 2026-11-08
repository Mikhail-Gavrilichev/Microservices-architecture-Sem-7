using System.Data;
using CheckingService.Data;
using CheckingService.Models;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace CheckingService.Services;

public class BusinessException(int code, string message) : Exception(message)
{
    public int Code { get; } = code;
}

// Демонстрационный серверный каталог: никакой общей БД с сервисом подготовки форм.
// В реальной интеграции эти данные поступали бы из доверенного API подготовки форм.
public static class TaskCatalog
{
    public record Entry(int GameId, int FormId, int TaskId, string Condition, string CorrectAnswer);
    public static readonly Entry[] Items =
    [
        new(1, 101, 1, "Назовите столицу Франции.", "Париж"),
        new(1, 102, 2, "Сколько будет 7 × 8?", "56"),
        new(1, 103, 3, "Какой химический символ у воды?", "H₂O")
    ];
}

public class CheckingWorkflow(AppDbContext db, IWebHostEnvironment environment)
{
    private static BusinessException Bad(string message) => new(400, message);
    private static BusinessException Missing(string name) => new(404, $"{name} не найден(а).");
    private static BusinessException Conflict(string message) => new(409, message);
    private string PhotoDirectory => Path.Combine(environment.ContentRootPath, "photos");
    private static bool ValidImage(byte[] bytes, string extension)
    {
        using var codec = SKCodec.Create(new MemoryStream(bytes));
        if (codec == null || codec.EncodedFormat != (extension == ".png" ? SKEncodedImageFormat.Png : SKEncodedImageFormat.Jpeg))
            return false;
        using var bitmap = SKBitmap.Decode(bytes);
        return bitmap != null && bitmap.Width > 0 && bitmap.Height > 0;
    }

    public static object SubmissionView(Submission s) => new
    {
        s.Id, s.TeamId, s.GameId, s.FormId, s.TaskId, s.Answer, s.PhotoPath,
        s.PhotoFormId, s.TaskCondition, s.Status, s.CreatedAt
    };

    // Умышленно без TeamId / названия команды / рейтинга — используется ВСЕМИ маршрутами жюри.
    public static object JudgeView(CheckRequest c) => new
    {
        c.Id, c.SubmissionId, c.JudgeId, c.Status, c.CreatedAt, c.AssignedAt, c.CompletedAt,
        c.Submission.GameId, c.Submission.FormId, c.Submission.TaskId,
        c.Submission.TaskCondition, c.Submission.Answer, c.Submission.CorrectAnswer,
        PhotoUrl = c.Submission.PhotoPath == null ? null : $"/api/checks/{c.Id}/photo"
    };

    public async Task<Submission> Submit(SubmitRequest input)
    {
        if (string.IsNullOrWhiteSpace(input.TeamId) || string.IsNullOrWhiteSpace(input.Answer))
            throw Bad("TeamId и ответ обязательны.");
        var task = TaskCatalog.Items.FirstOrDefault(x => x.GameId == input.GameId && x.FormId == input.FormId && x.TaskId == input.TaskId)
            ?? throw Bad("Комбинация GameId, FormId и TaskId отсутствует в серверном каталоге форм.");
        var submission = new Submission
        {
            TeamId = input.TeamId.Trim(), GameId = input.GameId, FormId = input.FormId, TaskId = input.TaskId,
            Answer = input.Answer.Trim(), TaskCondition = task.Condition, CorrectAnswer = task.CorrectAnswer
        };
        db.Submissions.Add(submission);
        await db.SaveChangesAsync();
        return submission;
    }

    public async Task<Submission> UploadPhoto(int id, UploadPhotoRequest input)
    {
        var submission = await db.Submissions.FindAsync(id) ?? throw Missing("Решение");
        if (submission.Status is not (SubmissionStatus.Created or SubmissionStatus.PhotoInvalid or SubmissionStatus.PhotoAttached))
            throw Conflict("Фотографию можно заменить только до принятия решения на проверку.");
        if (input.PhotoFormId != submission.FormId)
        {
            submission.Status = SubmissionStatus.PhotoInvalid;
            await db.SaveChangesAsync();
            throw Bad("Указанный на фотографии FormId не соответствует форме решения.");
        }
        var file = input.File ?? throw Bad("Передайте файл фотографии.");
        if (file.Length is < 1 or > 8_000_000)
        {
            submission.Status = SubmissionStatus.PhotoInvalid;
            await db.SaveChangesAsync();
            throw Bad("Размер фотографии должен быть от 1 байта до 8 МБ.");
        }
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png"))
        {
            submission.Status = SubmissionStatus.PhotoInvalid;
            await db.SaveChangesAsync();
            throw Bad("Допустимы только JPEG и PNG.");
        }

        // Декодируем ВСЁ изображение, не доверяя имени файла и Content-Type клиента.
        await using var source = file.OpenReadStream();
        using var buffer = new MemoryStream();
        await source.CopyToAsync(buffer);
        buffer.Position = 0;
        try
        {
            if (!ValidImage(buffer.ToArray(), extension))
                throw Bad("Файл повреждён или его формат не совпадает с расширением JPEG/PNG.");
        }
        catch (BusinessException)
        {
            submission.Status = SubmissionStatus.PhotoInvalid;
            await db.SaveChangesAsync();
            throw;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            submission.Status = SubmissionStatus.PhotoInvalid;
            await db.SaveChangesAsync();
            throw Bad("Фотографию не удалось открыть как изображение JPEG/PNG.");
        }

        Directory.CreateDirectory(PhotoDirectory);
        var filename = $"{Guid.NewGuid():N}{extension}";
        var newPath = Path.Combine(PhotoDirectory, filename);
        await File.WriteAllBytesAsync(newPath, buffer.ToArray());
        var previous = submission.PhotoPath;
        submission.PhotoPath = filename; // Только сгенерированное сервером имя, без пользовательского пути.
        submission.PhotoFormId = input.PhotoFormId;
        submission.Status = SubmissionStatus.PhotoAttached;
        try { await db.SaveChangesAsync(); }
        catch { File.Delete(newPath); throw; }
        if (previous != null && previous != filename) File.Delete(Path.Combine(PhotoDirectory, previous));
        return submission;
    }

    public async Task<Submission> ValidatePhoto(int id, ValidatePhotoRequest input)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var submission = await db.Submissions.FindAsync(id) ?? throw Missing("Решение");
        if (submission.Status != SubmissionStatus.PhotoAttached) throw Conflict("Сначала прикрепите фотографию.");
        var path = submission.PhotoPath == null ? null : Path.Combine(PhotoDirectory, submission.PhotoPath);
        var fileExists = path != null && File.Exists(path);
        var matchesForm = submission.PhotoFormId == submission.FormId;
        if (!fileExists || !matchesForm || string.IsNullOrWhiteSpace(submission.Answer)
            || input.IsPhotoReadable != true || input.IsAnswerVisible != true)
        {
            submission.Status = SubmissionStatus.PhotoInvalid;
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            throw Bad("Фото отклонено: проверьте файл, FormId, ответ, читаемость и видимость ответа. Исправьте и отправьте повторно.");
        }
        // Повторно открываем сохранённый файл: загрузка могла быть повреждена после записи.
        bool valid;
        try { valid = ValidImage(await File.ReadAllBytesAsync(path!), Path.GetExtension(path!).ToLowerInvariant()); }
        catch (Exception ex) when (ex is not OutOfMemoryException) { valid = false; }
        if (!valid)
        {
            submission.Status = SubmissionStatus.PhotoInvalid;
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            throw Bad("Сохранённая фотография повреждена. Отправьте новое изображение.");
        }
        submission.Status = SubmissionStatus.AcceptedForChecking;
        db.CheckRequests.Add(new CheckRequest { SubmissionId = id });
        await db.SaveChangesAsync();
        await Dispatch();
        await tx.CommitAsync();
        return submission;
    }

    public async Task<Submission> Resubmit(int id, ResubmitRequest input)
    {
        var s = await db.Submissions.FindAsync(id) ?? throw Missing("Решение");
        if (s.Status != SubmissionStatus.PhotoInvalid) throw Conflict("Повторная отправка возможна только после отклонения фото.");
        if (string.IsNullOrWhiteSpace(input.Answer)) throw Bad("Ответ не может быть пустым.");
        var previous = s.PhotoPath;
        s.Answer = input.Answer.Trim(); s.PhotoPath = null; s.PhotoFormId = null; s.Status = SubmissionStatus.Created;
        await db.SaveChangesAsync();
        if (previous != null) File.Delete(Path.Combine(PhotoDirectory, previous));
        return s;
    }

    public async Task<CheckRequest> CreateCheck(int submissionId)
    {
        var s = await db.Submissions.FindAsync(submissionId) ?? throw Missing("Решение");
        if (s.Status != SubmissionStatus.AcceptedForChecking) throw Conflict("Проверка создаётся только после подтверждения фотографии.");
        var existing = await db.CheckRequests.FirstOrDefaultAsync(c => c.SubmissionId == submissionId);
        if (existing != null) return existing; // Идемпотентно: проверка уже создана при подтверждении фото.
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var check = new CheckRequest { SubmissionId = submissionId };
        db.CheckRequests.Add(check);
        await db.SaveChangesAsync(); await Dispatch(); await tx.CommitAsync();
        return check;
    }

    public async Task<Judge> CreateJudge(CreateJudgeRequest input)
    {
        if (string.IsNullOrWhiteSpace(input.Name)) throw Bad("Имя члена жюри обязательно.");
        if (!Enum.IsDefined(input.Role)) throw Bad("Неизвестная роль жюри.");
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var judge = new Judge { Name = input.Name.Trim(), Role = input.Role, IsAvailable = input.IsAvailable };
        db.Judges.Add(judge);
        await db.SaveChangesAsync();
        await Dispatch(); await tx.CommitAsync();
        return judge;
    }

    private async Task Dispatch()
    {
        var judges = await db.Judges.Where(j => j.IsAvailable).OrderBy(j => j.Id).ToListAsync();
        var busy = (await db.CheckRequests.Where(c => c.JudgeId != null &&
            (c.Status == CheckStatus.Assigned || c.Status == CheckStatus.Accepted || c.Status == CheckStatus.Escalated))
            .Select(c => c.JudgeId!.Value).ToListAsync()).ToHashSet();
        var waiting = await db.CheckRequests.Where(c => c.Status == CheckStatus.Queued ||
            (c.Status == CheckStatus.Escalated && c.JudgeId == null))
            .OrderBy(c => c.Status == CheckStatus.Escalated ? 0 : 1).ThenBy(c => c.CreatedAt).ToListAsync();
        foreach (var check in waiting)
        {
            var judge = check.Status == CheckStatus.Escalated
                ? judges.FirstOrDefault(j => j.Role == JudgeRole.SeniorJudge && !busy.Contains(j.Id))
                : judges.FirstOrDefault(j => j.Role == JudgeRole.Judge && !busy.Contains(j.Id))
                    ?? judges.FirstOrDefault(j => j.Role == JudgeRole.SeniorJudge && !busy.Contains(j.Id));
            if (judge == null) continue;
            check.JudgeId = judge.Id;
            check.AssignedAt = DateTime.UtcNow;
            if (check.Status == CheckStatus.Queued) check.Status = CheckStatus.Assigned;
            busy.Add(judge.Id);
        }
        await db.SaveChangesAsync();
    }

    private async Task<Judge> AvailableJudge(int id, bool seniorOnly)
    {
        var judge = await db.Judges.FindAsync(id) ?? throw Missing("Член жюри");
        if (!judge.IsAvailable) throw Conflict("Член жюри недоступен.");
        if (seniorOnly && judge.Role != JudgeRole.SeniorJudge) throw Conflict("Переданную работу может принять только старший член жюри.");
        if (await db.CheckRequests.AnyAsync(c => c.JudgeId == id &&
            (c.Status == CheckStatus.Assigned || c.Status == CheckStatus.Accepted || c.Status == CheckStatus.Escalated)))
            throw Conflict("У члена жюри уже есть активная проверка.");
        return judge;
    }

    public async Task<CheckRequest> Assign(int id, int judgeId)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var check = await db.CheckRequests.FindAsync(id) ?? throw Missing("Запрос на проверку");
        if (check.Status != CheckStatus.Queued && !(check.Status == CheckStatus.Escalated && check.JudgeId == null))
            throw Conflict("Назначать можно только ожидающую или переданную старшему работу.");
        await AvailableJudge(judgeId, check.Status == CheckStatus.Escalated);
        check.JudgeId = judgeId; check.AssignedAt = DateTime.UtcNow;
        if (check.Status == CheckStatus.Queued) check.Status = CheckStatus.Assigned;
        await db.SaveChangesAsync(); await tx.CommitAsync();
        return check;
    }

    private async Task<CheckRequest> Owned(int id, int judgeId)
    {
        var check = await db.CheckRequests.FindAsync(id) ?? throw Missing("Запрос на проверку");
        if (!await db.Judges.AnyAsync(j => j.Id == judgeId)) throw Missing("Член жюри");
        if (check.JudgeId != judgeId) throw Conflict("Работа назначена другому члену жюри.");
        return check;
    }

    public async Task<CheckRequest> Accept(int id, int judgeId)
    {
        var check = await Owned(id, judgeId);
        if (check.Status != CheckStatus.Assigned && check.Status != CheckStatus.Escalated)
            throw Conflict("Принять можно только назначенную работу.");
        check.Status = CheckStatus.Accepted;
        await db.SaveChangesAsync();
        return check;
    }

    public async Task<CheckRequest> Decline(int id, int judgeId)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var check = await Owned(id, judgeId);
        if (check.Status != CheckStatus.Assigned) throw Conflict("Отказаться можно только от назначенной работы до её принятия.");
        // После отказа работа сразу становится Escalated и переназначается свободному SeniorJudge.
        // Если старший занят, она остаётся в очереди эскалации без JudgeId.
        check.Status = CheckStatus.Escalated;
        check.JudgeId = null; check.AssignedAt = null;
        await db.SaveChangesAsync();
        await Dispatch(); await tx.CommitAsync();
        return check;
    }

    public async Task<CheckRequest> Escalate(int id, int? seniorId)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var check = await db.CheckRequests.FindAsync(id) ?? throw Missing("Запрос на проверку");
        if (check.Status != CheckStatus.Escalated || check.JudgeId != null)
            throw Conflict("Эскалация доступна только для переданной работы, ещё не назначенной старшему.");
        if (seniorId.HasValue)
        {
            await AvailableJudge(seniorId.Value, true);
            check.JudgeId = seniorId.Value; check.AssignedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }
        else await Dispatch();
        await tx.CommitAsync();
        return check;
    }

    public async Task<CheckResult> Complete(int id, CompleteRequest input)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var check = await Owned(id, input.JudgeId);
        if (check.Status != CheckStatus.Accepted) throw Conflict("Завершить можно только принятую работу; повторная правка результата запрещена.");
        if (input.PointsAwarded < 0) throw Bad("Баллы не могут быть отрицательными.");
        if (await db.CheckResults.AnyAsync(r => r.CheckRequestId == id)) throw Conflict("Результат уже записан.");
        var submission = await db.Submissions.FindAsync(check.SubmissionId) ?? throw Missing("Решение");
        var result = new CheckResult { CheckRequestId = id, IsCorrect = input.IsCorrect,
            PointsAwarded = input.PointsAwarded, Comment = input.Comment?.Trim() ?? "" };
        db.CheckResults.Add(result);
        check.Status = CheckStatus.Completed; check.CompletedAt = DateTime.UtcNow;
        submission.Status = SubmissionStatus.Completed;
        await db.SaveChangesAsync();
        await Dispatch(); await tx.CommitAsync();
        return result;
    }

    public async Task<string> PhotoPathForCheck(int id)
    {
        var filename = await db.CheckRequests.Where(c => c.Id == id).Select(c => c.Submission.PhotoPath).FirstOrDefaultAsync();
        if (filename == null) throw Missing("Фотография или проверка");
        var path = Path.Combine(PhotoDirectory, Path.GetFileName(filename));
        if (!File.Exists(path)) throw Missing("Фотография");
        return path;
    }
}
