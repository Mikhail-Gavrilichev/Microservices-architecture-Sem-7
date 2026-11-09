using System.ComponentModel.DataAnnotations;

namespace CheckingService.Models;

public enum SubmissionStatus { Created, PhotoAttached, PhotoInvalid, AcceptedForChecking, Completed }
public enum CheckStatus { Queued, Assigned, Accepted, Declined, Escalated, Completed }
public enum JudgeRole { Judge, SeniorJudge }

public class Submission
{
    public int Id { get; set; }
    [MaxLength(100)] public string TeamId { get; set; } = "";
    public int GameId { get; set; }
    public int FormId { get; set; }
    public int TaskId { get; set; }
    public string Answer { get; set; } = "";
    public string? PhotoPath { get; set; }
    public int? PhotoFormId { get; set; }
    public string TaskCondition { get; set; } = "";
    public string CorrectAnswer { get; set; } = "";
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Created;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class CheckRequest
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public Submission Submission { get; set; } = null!;
    public int? JudgeId { get; set; }
    public Judge? Judge { get; set; }
    public CheckStatus Status { get; set; } = CheckStatus.Queued;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? AssignedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class Judge
{
    public int Id { get; set; }
    [MaxLength(150)] public string Name { get; set; } = "";
    public JudgeRole Role { get; set; } = JudgeRole.Judge;
    public bool IsAvailable { get; set; } = true;
}

public class CheckResult
{
    public int Id { get; set; }
    public int CheckRequestId { get; set; }
    public CheckRequest CheckRequest { get; set; } = null!;
    public bool IsCorrect { get; set; }
    public int PointsAwarded { get; set; }
    public string Comment { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
