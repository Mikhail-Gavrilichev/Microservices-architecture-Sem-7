using TaskService.Domain.Common;
using TaskService.Domain.GameTasks.Events;

namespace TaskService.Domain.GameTasks;

/// <summary>
/// Агрегат «Задача» из EventStorming.
///
/// Команды: создать задачу, изменить условие, изменить правильный ответ, опубликовать задачу.
/// События: задача создана, условие изменено, правильный ответ изменён, задача опубликована.
///
/// Инварианты:
///  - задача создаётся в статусе Draft;
///  - менять условие и правильный ответ можно только до публикации;
///  - опубликовать задачу можно только при заполненных условии и правильном ответе;
///  - опубликованная задача неизменяема (на неё ссылаются запросы на проверку).
/// </summary>
public sealed class GameTask : AggregateRoot
{
    public const int TitleMaxLength = 200;
    public const int StatementMaxLength = 4000;
    public const int CorrectAnswerMaxLength = 2000;

    // Для EF Core.
    private GameTask()
    {
    }

    private GameTask(
        Guid id,
        string title,
        string statement,
        string correctAnswer,
        DateTimeOffset now)
    {
        Id = id;
        Title = title;
        Statement = statement;
        CorrectAnswer = correctAnswer;
        Status = GameTaskStatus.Draft;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    /// <summary>Короткое название задачи для списков и форм. В диаграмме не выделено отдельной командой.</summary>
    public string Title { get; private set; } = string.Empty;

    /// <summary>Условие задачи (то, что видит участник и жюри).</summary>
    public string Statement { get; private set; } = string.Empty;

    /// <summary>Правильный ответ. Не должен попадать участникам до завершения проверки.</summary>
    public string CorrectAnswer { get; private set; } = string.Empty;

    public GameTaskStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? PublishedAt { get; private set; }

    public static GameTask Create(
        string title,
        string? statement,
        string? correctAnswer,
        DateTimeOffset now)
    {
        var normalizedTitle = NormalizeRequired(title, "Название задачи", TitleMaxLength);
        var normalizedStatement = NormalizeOptional(statement, "Условие задачи", StatementMaxLength);
        var normalizedAnswer = NormalizeOptional(correctAnswer, "Правильный ответ", CorrectAnswerMaxLength);

        var task = new GameTask(
            Guid.NewGuid(),
            normalizedTitle,
            normalizedStatement,
            normalizedAnswer,
            now);

        task.Raise(new TaskCreatedDomainEvent(task.Id, task.Title, now));
        return task;
    }

    /// <summary>Команда «Изменить условие».</summary>
    public void ChangeStatement(string statement, DateTimeOffset now)
    {
        EnsureDraft();

        var normalized = NormalizeRequired(statement, "Условие задачи", StatementMaxLength);
        if (string.Equals(Statement, normalized, StringComparison.Ordinal))
        {
            return;
        }

        Statement = normalized;
        Touch(now);
        Raise(new TaskStatementChangedDomainEvent(Id, now));
    }

    /// <summary>Команда «Изменить правильный ответ».</summary>
    public void ChangeCorrectAnswer(string correctAnswer, DateTimeOffset now)
    {
        EnsureDraft();

        var normalized = NormalizeRequired(correctAnswer, "Правильный ответ", CorrectAnswerMaxLength);
        if (string.Equals(CorrectAnswer, normalized, StringComparison.Ordinal))
        {
            return;
        }

        CorrectAnswer = normalized;
        Touch(now);
        Raise(new TaskCorrectAnswerChangedDomainEvent(Id, now));
    }

    /// <summary>Команда «Опубликовать задачу».</summary>
    public void Publish(DateTimeOffset now)
    {
        if (Status == GameTaskStatus.Published)
        {
            throw new DomainException("Задача уже опубликована.");
        }

        if (string.IsNullOrWhiteSpace(Statement))
        {
            throw new DomainException("Нельзя опубликовать задачу без условия.");
        }

        if (string.IsNullOrWhiteSpace(CorrectAnswer))
        {
            throw new DomainException("Нельзя опубликовать задачу без правильного ответа.");
        }

        Status = GameTaskStatus.Published;
        PublishedAt = now;
        Touch(now);
        Raise(new TaskPublishedDomainEvent(Id, now));
    }

    private void EnsureDraft()
    {
        if (Status != GameTaskStatus.Draft)
        {
            throw new DomainException("Опубликованную задачу нельзя изменять.");
        }
    }

    private void Touch(DateTimeOffset now) => UpdatedAt = now;

    private static string NormalizeRequired(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} обязательно для заполнения.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} не должно превышать {maxLength} символов.");
        }

        return normalized;
    }

    private static string NormalizeOptional(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} не должно превышать {maxLength} символов.");
        }

        return normalized;
    }
}
