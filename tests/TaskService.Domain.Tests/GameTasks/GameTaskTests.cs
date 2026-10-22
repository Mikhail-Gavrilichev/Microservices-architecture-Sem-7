using TaskService.Domain.Common;
using TaskService.Domain.GameTasks;
using TaskService.Domain.GameTasks.Events;

namespace TaskService.Domain.Tests.GameTasks;

public sealed class GameTaskTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_starts_as_draft_and_raises_created_event()
    {
        var task = GameTask.Create("Задача 1", "Столица Австралии?", "Канберра", Now);

        Assert.Equal(GameTaskStatus.Draft, task.Status);
        Assert.Equal("Задача 1", task.Title);
        Assert.Equal("Столица Австралии?", task.Statement);
        Assert.Equal("Канберра", task.CorrectAnswer);
        Assert.Equal(Now, task.CreatedAt);
        Assert.Equal(Now, task.UpdatedAt);
        Assert.IsType<TaskCreatedDomainEvent>(Assert.Single(task.DomainEvents));
    }

    [Fact]
    public void Create_without_title_throws()
    {
        Assert.Throws<DomainException>(() =>
            GameTask.Create("   ", "Условие", "Ответ", Now));
    }

    [Fact]
    public void Publish_without_statement_throws()
    {
        var task = GameTask.Create("Задача 1", null, "Канберра", Now);

        var exception = Assert.Throws<DomainException>(() => task.Publish(Now));

        Assert.Contains("без условия", exception.Message);
    }

    [Fact]
    public void Publish_without_correct_answer_throws()
    {
        var task = GameTask.Create("Задача 1", "Столица Австралии?", null, Now);

        var exception = Assert.Throws<DomainException>(() => task.Publish(Now));

        Assert.Contains("без правильного ответа", exception.Message);
    }

    [Fact]
    public void Publish_sets_status_and_raises_event()
    {
        var task = GameTask.Create("Задача 1", "Столица Австралии?", "Канберра", Now);

        task.Publish(Now.AddMinutes(5));

        Assert.Equal(GameTaskStatus.Published, task.Status);
        Assert.Equal(Now.AddMinutes(5), task.PublishedAt);
        Assert.Single(task.DomainEvents.OfType<TaskPublishedDomainEvent>());
    }

    [Fact]
    public void Publish_twice_throws()
    {
        var task = GameTask.Create("Задача 1", "Условие", "Ответ", Now);
        task.Publish(Now);

        Assert.Throws<DomainException>(() => task.Publish(Now));
    }

    [Fact]
    public void Change_statement_on_published_task_throws()
    {
        var task = GameTask.Create("Задача 1", "Условие", "Ответ", Now);
        task.Publish(Now);

        Assert.Throws<DomainException>(() => task.ChangeStatement("Новое условие", Now));
    }

    [Fact]
    public void Change_statement_trims_value_and_raises_event()
    {
        var task = GameTask.Create("Задача 1", "Старое условие", "Ответ", Now);

        task.ChangeStatement("  Новое условие  ", Now.AddMinutes(1));

        Assert.Equal("Новое условие", task.Statement);
        Assert.Equal(Now.AddMinutes(1), task.UpdatedAt);
        Assert.Single(task.DomainEvents.OfType<TaskStatementChangedDomainEvent>());
    }

    [Fact]
    public void Change_statement_to_same_value_does_not_raise_event()
    {
        var task = GameTask.Create("Задача 1", "Условие", "Ответ", Now);
        var eventsBefore = task.DomainEvents.Count;

        task.ChangeStatement(" Условие ", Now.AddMinutes(1));

        Assert.Equal(eventsBefore, task.DomainEvents.Count);
        Assert.Equal(Now, task.UpdatedAt);
    }

    [Fact]
    public void Change_correct_answer_on_draft_raises_event()
    {
        var task = GameTask.Create("Задача 1", "Условие", "Старый ответ", Now);

        task.ChangeCorrectAnswer("Новый ответ", Now.AddMinutes(1));

        Assert.Equal("Новый ответ", task.CorrectAnswer);
        Assert.Single(task.DomainEvents.OfType<TaskCorrectAnswerChangedDomainEvent>());
    }

    [Fact]
    public void Change_correct_answer_to_empty_throws()
    {
        var task = GameTask.Create("Задача 1", "Условие", "Ответ", Now);

        Assert.Throws<DomainException>(() =>
            task.ChangeCorrectAnswer("   ", Now.AddMinutes(1)));
    }
}
