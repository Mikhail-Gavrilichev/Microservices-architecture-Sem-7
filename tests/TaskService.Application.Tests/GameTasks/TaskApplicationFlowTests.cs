using Microsoft.EntityFrameworkCore;
using TaskService.Application.Common.Exceptions;
using TaskService.Application.GameTasks.Commands.ChangeTaskStatement;
using TaskService.Application.GameTasks.Commands.CreateTask;
using TaskService.Application.GameTasks.Commands.PublishTask;
using TaskService.Application.GameTasks.Queries.GetTaskSnapshot;
using TaskService.Application.GameTasks.Queries.ListTasks;
using TaskService.Domain.GameTasks;
using TaskService.Infrastructure.Persistence;
using TaskService.Infrastructure.Persistence.Repositories;

namespace TaskService.Application.Tests.GameTasks;

public sealed class TaskApplicationFlowTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_and_publish_task_flow()
    {
        await using var db = CreateDbContext();
        var repository = new GameTaskRepository(db);
        var unitOfWork = new UnitOfWork(db);
        var timeProvider = new FakeTimeProvider(Now);

        var createHandler = new CreateTaskCommandHandler(repository, unitOfWork, timeProvider);
        var created = await createHandler.Handle(
            new CreateTaskCommand("Задача 1", "Столица Австралии?", "Канберра"),
            CancellationToken.None);

        Assert.Equal(GameTaskStatus.Draft, created.Status);

        var publishHandler = new PublishTaskCommandHandler(repository, unitOfWork, timeProvider);
        await publishHandler.Handle(new PublishTaskCommand(created.Id), CancellationToken.None);

        var snapshotHandler = new GetTaskSnapshotQueryHandler(repository);
        var snapshot = await snapshotHandler.Handle(
            new GetTaskSnapshotQuery(created.Id),
            CancellationToken.None);

        Assert.Equal("Столица Австралии?", snapshot.Statement);
        Assert.Equal("Канберра", snapshot.CorrectAnswer);
        Assert.Equal(Now, snapshot.PublishedAt);
    }

    [Fact]
    public async Task Get_snapshot_for_draft_throws_conflict()
    {
        await using var db = CreateDbContext();
        var repository = new GameTaskRepository(db);
        var unitOfWork = new UnitOfWork(db);
        var timeProvider = new FakeTimeProvider(Now);

        var createHandler = new CreateTaskCommandHandler(repository, unitOfWork, timeProvider);
        var created = await createHandler.Handle(
            new CreateTaskCommand("Задача 1", "Условие", "Ответ"),
            CancellationToken.None);

        var snapshotHandler = new GetTaskSnapshotQueryHandler(repository);

        await Assert.ThrowsAsync<ConflictException>(() =>
            snapshotHandler.Handle(new GetTaskSnapshotQuery(created.Id), CancellationToken.None));
    }

    [Fact]
    public async Task Change_statement_after_publish_throws_domain_exception()
    {
        await using var db = CreateDbContext();
        var repository = new GameTaskRepository(db);
        var unitOfWork = new UnitOfWork(db);
        var timeProvider = new FakeTimeProvider(Now);

        var createHandler = new CreateTaskCommandHandler(repository, unitOfWork, timeProvider);
        var created = await createHandler.Handle(
            new CreateTaskCommand("Задача 1", "Условие", "Ответ"),
            CancellationToken.None);

        await new PublishTaskCommandHandler(repository, unitOfWork, timeProvider)
            .Handle(new PublishTaskCommand(created.Id), CancellationToken.None);

        var changeHandler = new ChangeTaskStatementCommandHandler(repository, unitOfWork, timeProvider);

        await Assert.ThrowsAsync<TaskService.Domain.Common.DomainException>(() =>
            changeHandler.Handle(
                new ChangeTaskStatementCommand(created.Id, "Новое условие"),
                CancellationToken.None));
    }

    [Fact]
    public async Task List_tasks_filters_by_status_and_search()
    {
        await using var db = CreateDbContext();
        var repository = new GameTaskRepository(db);
        var unitOfWork = new UnitOfWork(db);
        var timeProvider = new FakeTimeProvider(Now);
        var createHandler = new CreateTaskCommandHandler(repository, unitOfWork, timeProvider);

        var first = await createHandler.Handle(
            new CreateTaskCommand("География", "Столица Австралии?", "Канберра"),
            CancellationToken.None);
        await createHandler.Handle(
            new CreateTaskCommand("История", "Год основания Рима?", "753 до н.э."),
            CancellationToken.None);

        await new PublishTaskCommandHandler(repository, unitOfWork, timeProvider)
            .Handle(new PublishTaskCommand(first.Id), CancellationToken.None);

        var handler = new ListTasksQueryHandler(repository);

        var published = await handler.Handle(
            new ListTasksQuery(Status: GameTaskStatus.Published),
            CancellationToken.None);

        var search = await handler.Handle(
            new ListTasksQuery(Search: "Рим"),
            CancellationToken.None);

        Assert.Single(published.Items);
        Assert.Equal(first.Id, published.Items[0].Id);
        Assert.Single(search.Items);
        Assert.Equal("История", search.Items[0].Title);
    }

    private static TaskDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<TaskDbContext>()
            .UseInMemoryDatabase($"tasks-{Guid.NewGuid()}")
            .Options;

        return new TaskDbContext(options);
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
