using System.ComponentModel.DataAnnotations;

namespace GameFormsService.Models;

public enum GameStatus { Draft, Published }
public enum TaskStatus { Draft, Published }
public enum FormSetStatus { Created, Prepared, Printed, Issued }
public enum FormStatus { Created, Prepared, Printed }

public class Game
{
    public int Id { get; set; }
    [MaxLength(200)] public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Rules { get; set; } = "";
    public string FormRules { get; set; } = "";
    public GameStatus Status { get; set; } = GameStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class GameTask
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public Game Game { get; set; } = null!;
    [MaxLength(200)] public string Title { get; set; } = "";
    public string Condition { get; set; } = "";
    public string CorrectAnswer { get; set; } = "";
    public int Points { get; set; } = 1;
    public TaskStatus Status { get; set; } = TaskStatus.Draft;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class FormSet
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public Game Game { get; set; } = null!;
    [MaxLength(100)] public string TeamId { get; set; } = "";
    public FormSetStatus Status { get; set; } = FormSetStatus.Created;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class Form
{
    public int Id { get; set; }
    public int FormSetId { get; set; }
    public FormSet FormSet { get; set; } = null!;
    public int TaskId { get; set; }
    public GameTask Task { get; set; } = null!;
    public FormStatus Status { get; set; } = FormStatus.Created;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
