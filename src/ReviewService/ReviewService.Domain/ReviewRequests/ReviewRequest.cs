using ReviewService.Domain.Common;
using ReviewService.Domain.ReviewRequests.Events;

namespace ReviewService.Domain.ReviewRequests;

/// <summary>
/// Агрегат «Запрос на проверку» из EventStorming.
///
/// Команды: создать запрос на проверку, добавить в очередь, назначить проверку,
/// принять проверку, отказаться от проверки, передать старшему, завершить проверку.
///
/// События: запрос на проверку создан, запрос добавлен в очередь, проверка назначена,
/// проверка принята, проверка отклонена, проверка передана старшему, проверка завершена.
///
/// Ключевое правило доступа: жюри видит условие задачи, ответ участника и правильный
/// ответ, но не видит команду и рейтинг. TeamId хранится только для внутренних
/// интеграций (например, для расчёта счёта игры) и не попадает в jury-DTO.
/// </summary>
public sealed class ReviewRequest : AggregateRoot
{
    public const int TaskStatementMaxLength = 4000;
    public const int CorrectAnswerMaxLength = 2000;
    public const int ParticipantAnswerMaxLength = 4000;
    public const int RefusalReasonMaxLength = 1000;
    public const int CommentMaxLength = 2000;

    // Для EF Core.
    private ReviewRequest()
    {
    }

    private ReviewRequest(
        Guid id,
        Guid solutionId,
        Guid taskId,
        string taskStatement,
        string correctAnswer,
        string participantAnswer,
        Guid teamId,
        ReviewSource source,
        Guid? sourceReferenceId,
        DateTimeOffset now)
    {
        Id = id;
        SolutionId = solutionId;
        TaskId = taskId;
        TaskStatement = taskStatement;
        CorrectAnswer = correctAnswer;
        ParticipantAnswer = participantAnswer;
        TeamId = teamId;
        Source = source;
        SourceReferenceId = sourceReferenceId;
        Status = ReviewStatus.Created;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    /// <summary>Ссылка на решение (агрегат «Решение»), не является внешним ключом этого сервиса.</summary>
    public Guid SolutionId { get; private set; }

    /// <summary>Ссылка на задачу в TaskService.</summary>
    public Guid TaskId { get; private set; }

    /// <summary>Снимок условия задачи на момент создания запроса.</summary>
    public string TaskStatement { get; private set; } = string.Empty;

    /// <summary>Снимок правильного ответа на момент создания запроса.</summary>
    public string CorrectAnswer { get; private set; } = string.Empty;

    /// <summary>Ответ участника, извлечённый из решения.</summary>
    public string ParticipantAnswer { get; private set; } = string.Empty;

    /// <summary>Внутренний идентификатор команды. Никогда не отдаётся жюри.</summary>
    public Guid TeamId { get; private set; }

    public ReviewSource Source { get; private set; }

    /// <summary>Идентификатор апелляции, если проверка инициирована повторно.</summary>
    public Guid? SourceReferenceId { get; private set; }

    public ReviewStatus Status { get; private set; }

    public Guid? AssignedJuryMemberId { get; private set; }

    public string? RefusalReason { get; private set; }

    public ReviewVerdict? Verdict { get; private set; }

    public string? Comment { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public DateTimeOffset? QueuedAt { get; private set; }

    public DateTimeOffset? AssignedAt { get; private set; }

    public DateTimeOffset? AcceptedAt { get; private set; }

    public DateTimeOffset? RefusedAt { get; private set; }

    public DateTimeOffset? TransferredAt { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public static ReviewRequest Create(
        Guid solutionId,
        Guid taskId,
        string taskStatement,
        string correctAnswer,
        string participantAnswer,
        Guid teamId,
        ReviewSource source,
        Guid? sourceReferenceId,
        DateTimeOffset now)
    {
        EnsureNotEmpty(solutionId, "Решение");
        EnsureNotEmpty(taskId, "Задача");
        EnsureNotEmpty(teamId, "Команда");

        if (source == ReviewSource.Appeal && sourceReferenceId is null)
        {
            throw new DomainException("Для повторной проверки по апелляции необходимо указать идентификатор апелляции.");
        }

        var request = new ReviewRequest(
            Guid.NewGuid(),
            solutionId,
            taskId,
            NormalizeRequired(taskStatement, "Условие задачи", TaskStatementMaxLength),
            NormalizeRequired(correctAnswer, "Правильный ответ", CorrectAnswerMaxLength),
            NormalizeRequired(participantAnswer, "Ответ участника", ParticipantAnswerMaxLength),
            teamId,
            source,
            sourceReferenceId,
            now);

        request.Raise(new ReviewRequestCreatedDomainEvent(
            request.Id,
            request.TaskId,
            request.TeamId,
            request.Source,
            now));

        return request;
    }

    /// <summary>Команда «Добавить в очередь».</summary>
    public void Enqueue(DateTimeOffset now)
    {
        if (Status != ReviewStatus.Created)
        {
            throw new DomainException("В очередь можно добавить только что созданный запрос.");
        }

        Status = ReviewStatus.Queued;
        QueuedAt = now;
        Touch(now);
        Raise(new ReviewRequestQueuedDomainEvent(Id, now));
    }

    /// <summary>Команда «Назначить проверку».</summary>
    public void Assign(Guid juryMemberId, DateTimeOffset now)
    {
        EnsureNotEmpty(juryMemberId, "Член жюри");

        if (Status != ReviewStatus.Queued)
        {
            throw new DomainException("Назначить проверку можно только для запроса из очереди.");
        }

        Status = ReviewStatus.Assigned;
        AssignedJuryMemberId = juryMemberId;
        AssignedAt = now;
        Touch(now);
        Raise(new ReviewAssignedDomainEvent(Id, juryMemberId, now));
    }

    /// <summary>Команда «Принять проверку».</summary>
    public void Accept(Guid juryMemberId, DateTimeOffset now)
    {
        EnsureAssignedTo(juryMemberId);

        if (Status is not (ReviewStatus.Assigned or ReviewStatus.TransferredToSenior))
        {
            throw new DomainException("Принять можно только назначенную или переданную проверку.");
        }

        Status = ReviewStatus.Accepted;
        AcceptedAt = now;
        Touch(now);
        Raise(new ReviewAcceptedDomainEvent(Id, juryMemberId, now));
    }

    /// <summary>Команда «Отказаться от проверки».</summary>
    public void Refuse(Guid juryMemberId, string reason, DateTimeOffset now)
    {
        EnsureAssignedTo(juryMemberId);

        if (Status is not (ReviewStatus.Assigned or ReviewStatus.Accepted or ReviewStatus.TransferredToSenior))
        {
            throw new DomainException("Отказаться можно только от назначенной, принятой или переданной проверки.");
        }

        Status = ReviewStatus.Refused;
        RefusalReason = NormalizeRequired(reason, "Причина отказа", RefusalReasonMaxLength);
        RefusedAt = now;
        Touch(now);
        Raise(new ReviewRefusedDomainEvent(Id, juryMemberId, RefusalReason, now));
    }

    /// <summary>Команда «Передать старшему» (политика после отказа).</summary>
    public void TransferToSenior(Guid seniorJuryMemberId, DateTimeOffset now)
    {
        EnsureNotEmpty(seniorJuryMemberId, "Старший член жюри");

        if (Status != ReviewStatus.Refused)
        {
            throw new DomainException("Передать старшему можно только проверку, от которой отказались.");
        }

        if (AssignedJuryMemberId == seniorJuryMemberId)
        {
            throw new DomainException("Проверку нельзя передать тому же члену жюри.");
        }

        Status = ReviewStatus.TransferredToSenior;
        AssignedJuryMemberId = seniorJuryMemberId;
        TransferredAt = now;
        Touch(now);
        Raise(new ReviewTransferredToSeniorDomainEvent(Id, seniorJuryMemberId, now));
    }

    /// <summary>Команда «Завершить проверку».</summary>
    public void Complete(Guid juryMemberId, ReviewVerdict verdict, string? comment, DateTimeOffset now)
    {
        EnsureAssignedTo(juryMemberId);

        if (Status is not (ReviewStatus.Assigned or ReviewStatus.Accepted or ReviewStatus.TransferredToSenior))
        {
            throw new DomainException("Завершить можно только назначенную, принятую или переданную проверку.");
        }

        Status = ReviewStatus.Completed;
        Verdict = verdict;
        Comment = NormalizeOptional(comment, "Комментарий", CommentMaxLength);
        CompletedAt = now;
        Touch(now);
        Raise(new ReviewCompletedDomainEvent(Id, TeamId, juryMemberId, verdict, now));
    }

    private void EnsureAssignedTo(Guid juryMemberId)
    {
        EnsureNotEmpty(juryMemberId, "Член жюри");

        if (AssignedJuryMemberId != juryMemberId)
        {
            throw new DomainException("Проверка не назначена этому члену жюри.");
        }
    }

    private void Touch(DateTimeOffset now) => UpdatedAt = now;

    private static void EnsureNotEmpty(Guid value, string fieldName)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{fieldName}: идентификатор обязателен.");
        }
    }

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

    private static string? NormalizeOptional(string? value, string fieldName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} не должно превышать {maxLength} символов.");
        }

        return normalized;
    }
}
