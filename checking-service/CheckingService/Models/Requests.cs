using System.ComponentModel.DataAnnotations;

namespace CheckingService.Models;

public record SubmitRequest(
    [Required, StringLength(100)] string TeamId,
    [Range(1, int.MaxValue)] int GameId,
    [Range(1, int.MaxValue)] int FormId,
    [Range(1, int.MaxValue)] int TaskId,
    [Required] string Answer);

public class UploadPhotoRequest
{
    [Required] public IFormFile? File { get; set; }
    [Range(1, int.MaxValue)] public int PhotoFormId { get; set; }
}

public record ValidatePhotoRequest([Required] bool? IsPhotoReadable, [Required] bool? IsAnswerVisible);
public record ResubmitRequest([Required] string Answer);
public record CreateCheckRequest([Range(1, int.MaxValue)] int SubmissionId);
public record AssignRequest([Required, Range(1, int.MaxValue)] int? JudgeId);
public record JudgeActionRequest([Range(1, int.MaxValue)] int JudgeId);
public record CompleteRequest([Range(1, int.MaxValue)] int JudgeId, bool IsCorrect, [Range(0, 10000)] int PointsAwarded, string? Comment);
public record CreateJudgeRequest([Required, StringLength(150)] string Name, JudgeRole Role, bool IsAvailable = true);
