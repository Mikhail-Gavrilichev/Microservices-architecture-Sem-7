using Microsoft.EntityFrameworkCore;
using ReviewService.Application.Common.Exceptions;
using ReviewService.Application.Common.Interfaces;
using ReviewService.Application.Common.Models;
using ReviewService.Application.ReviewRequests.Commands.AcceptReview;
using ReviewService.Application.ReviewRequests.Commands.AddToQueue;
using ReviewService.Application.ReviewRequests.Commands.AssignNextReview;
using ReviewService.Application.ReviewRequests.Commands.AssignReview;
using ReviewService.Application.ReviewRequests.Commands.CompleteReview;
using ReviewService.Application.ReviewRequests.Commands.CreateReviewRequest;
using ReviewService.Application.ReviewRequests.Commands.RefuseReview;
using ReviewService.Application.ReviewRequests.Commands.TransferReviewToSenior;
using ReviewService.Application.ReviewRequests.Dtos;
using ReviewService.Application.ReviewRequests.Queries.GetJuryReview;
using ReviewService.Domain.ReviewRequests;
using ReviewService.Infrastructure.Persistence;
using ReviewService.Infrastructure.Persistence.Repositories;

namespace ReviewService.Application.Tests.ReviewRequests;

public sealed class ReviewApplicationFlowTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Create_uses_task_snapshot_and_keeps_team_internal()
    {
        await using var db = CreateDbContext();
        var repository = new ReviewRequestRepository(db);
        var unitOfWork = new UnitOfWork(db);
        var timeProvider = new FakeTimeProvider(Now);
        var taskId = Guid.NewGuid();
        var teamId = Guid.NewGuid();
        var taskCatalog = new FakeTaskCatalog(taskId, "Условие из TaskService", "Правильный ответ");

        var handler = new CreateReviewRequestCommandHandler(
            repository,
            unitOfWork,
            taskCatalog,
            timeProvider);

        var details = await handler.Handle(
            new CreateReviewRequestCommand(
                Guid.NewGuid(),
                taskId,
                "Ответ участника",
                teamId),
            CancellationToken.None);

        Assert.Equal("Условие из TaskService", details.TaskStatement);
        Assert.Equal("Правильный ответ", details.CorrectAnswer);
        Assert.Equal(teamId, details.TeamId);

        var stored = await repository.GetByIdAsync(details.Id, CancellationToken.None);
        Assert.NotNull(stored);

        var juryDto = JuryReviewDto.FromDomain(stored);
        Assert.Equal("Условие из TaskService", juryDto.TaskStatement);

        var juryProperties = typeof(JuryReviewDto).GetProperties().Select(property => property.Name).ToArray();
        Assert.DoesNotContain("TeamId", juryProperties);
        Assert.DoesNotContain("SolutionId", juryProperties);
        Assert.DoesNotContain("SourceReferenceId", juryProperties);
        Assert.DoesNotContain("Rating", juryProperties);

        var queueProperties = typeof(ReviewQueueItemDto).GetProperties().Select(property => property.Name).ToArray();
        Assert.DoesNotContain("TeamId", queueProperties);
        Assert.DoesNotContain("SolutionId", queueProperties);
    }

    [Fact]
    public async Task Create_when_task_is_missing_propagates_not_found()
    {
        await using var db = CreateDbContext();
        var repository = new ReviewRequestRepository(db);
        var unitOfWork = new UnitOfWork(db);
        var taskCatalog = new FakeTaskCatalog(Guid.NewGuid(), "Условие", "Ответ");
        var handler = new CreateReviewRequestCommandHandler(
            repository,
            unitOfWork,
            taskCatalog,
            new FakeTimeProvider(Now));

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(
            new CreateReviewRequestCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                "Ответ участника",
                Guid.NewGuid()),
            CancellationToken.None));
    }

    [Fact]
    public async Task Full_refuse_and_senior_transfer_flow_completes_review()
    {
        await using var db = CreateDbContext();
        var repository = new ReviewRequestRepository(db);
        var unitOfWork = new UnitOfWork(db);
        var timeProvider = new FakeTimeProvider(Now);
        var taskCatalog = new FakeTaskCatalog();

        var createHandler = new CreateReviewRequestCommandHandler(
            repository,
            unitOfWork,
            taskCatalog,
            timeProvider);

        var details = await createHandler.Handle(
            new CreateReviewRequestCommand(
                Guid.NewGuid(),
                taskCatalog.TaskId,
                "Неверный ответ",
                Guid.NewGuid()),
            CancellationToken.None);

        await new AddReviewRequestToQueueCommandHandler(repository, unitOfWork, timeProvider)
            .Handle(new AddReviewRequestToQueueCommand(details.Id), CancellationToken.None);

        var juryMemberId = Guid.NewGuid();
        await new AssignReviewCommandHandler(repository, unitOfWork, timeProvider)
            .Handle(new AssignReviewCommand(details.Id, juryMemberId), CancellationToken.None);

        await new RefuseReviewCommandHandler(repository, unitOfWork, timeProvider)
            .Handle(new RefuseReviewCommand(details.Id, juryMemberId, "Конфликт интересов"), CancellationToken.None);

        var seniorId = Guid.NewGuid();
        await new TransferReviewToSeniorCommandHandler(repository, unitOfWork, timeProvider)
            .Handle(new TransferReviewToSeniorCommand(details.Id, seniorId), CancellationToken.None);

        await new AcceptReviewCommandHandler(repository, unitOfWork, timeProvider)
            .Handle(new AcceptReviewCommand(details.Id, seniorId), CancellationToken.None);

        await new CompleteReviewCommandHandler(repository, unitOfWork, timeProvider)
            .Handle(new CompleteReviewCommand(details.Id, seniorId, ReviewVerdict.Incorrect, "Ответ неверный"),
                CancellationToken.None);

        var stored = await repository.GetByIdAsync(details.Id, CancellationToken.None);
        Assert.NotNull(stored);
        Assert.Equal(ReviewStatus.Completed, stored.Status);
        Assert.Equal(ReviewVerdict.Incorrect, stored.Verdict);
        Assert.Equal(seniorId, stored.AssignedJuryMemberId);
    }

    [Fact]
    public async Task Assign_next_takes_oldest_queued_request()
    {
        await using var db = CreateDbContext();
        var repository = new ReviewRequestRepository(db);
        var unitOfWork = new UnitOfWork(db);
        var timeProvider = new FakeTimeProvider(Now);
        var taskCatalog = new FakeTaskCatalog();
        var createHandler = new CreateReviewRequestCommandHandler(
            repository,
            unitOfWork,
            taskCatalog,
            timeProvider);
        var queueHandler = new AddReviewRequestToQueueCommandHandler(repository, unitOfWork, timeProvider);

        var first = await createHandler.Handle(
            new CreateReviewRequestCommand(Guid.NewGuid(), taskCatalog.TaskId, "Первый", Guid.NewGuid()),
            CancellationToken.None);
        timeProvider.UtcNow = Now.AddMinutes(1);
        var second = await createHandler.Handle(
            new CreateReviewRequestCommand(Guid.NewGuid(), taskCatalog.TaskId, "Второй", Guid.NewGuid()),
            CancellationToken.None);

        await queueHandler.Handle(new AddReviewRequestToQueueCommand(second.Id), CancellationToken.None);
        timeProvider.UtcNow = Now.AddMinutes(2);
        await queueHandler.Handle(new AddReviewRequestToQueueCommand(first.Id), CancellationToken.None);

        var juryMemberId = Guid.NewGuid();
        var assigned = await new AssignNextReviewCommandHandler(repository, unitOfWork, timeProvider)
            .Handle(new AssignNextReviewCommand(juryMemberId), CancellationToken.None);

        Assert.NotNull(assigned);
        Assert.Equal(second.Id, assigned.Id);
        Assert.Equal(ReviewStatus.Assigned, assigned.Status);
    }

    [Fact]
    public async Task Jury_cannot_read_review_assigned_to_another_member()
    {
        await using var db = CreateDbContext();
        var repository = new ReviewRequestRepository(db);
        var unitOfWork = new UnitOfWork(db);
        var timeProvider = new FakeTimeProvider(Now);
        var taskCatalog = new FakeTaskCatalog();
        var createHandler = new CreateReviewRequestCommandHandler(
            repository,
            unitOfWork,
            taskCatalog,
            timeProvider);

        var details = await createHandler.Handle(
            new CreateReviewRequestCommand(Guid.NewGuid(), taskCatalog.TaskId, "Ответ", Guid.NewGuid()),
            CancellationToken.None);

        await new AddReviewRequestToQueueCommandHandler(repository, unitOfWork, timeProvider)
            .Handle(new AddReviewRequestToQueueCommand(details.Id), CancellationToken.None);
        var juryMemberId = Guid.NewGuid();
        await new AssignReviewCommandHandler(repository, unitOfWork, timeProvider)
            .Handle(new AssignReviewCommand(details.Id, juryMemberId), CancellationToken.None);

        var handler = new GetJuryReviewQueryHandler(repository);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetJuryReviewQuery(details.Id, Guid.NewGuid()), CancellationToken.None));

        var ownReview = await handler.Handle(
            new GetJuryReviewQuery(details.Id, juryMemberId),
            CancellationToken.None);

        Assert.Equal(details.Id, ownReview.Id);
    }

    private static ReviewDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<ReviewDbContext>()
            .UseInMemoryDatabase($"reviews-{Guid.NewGuid()}")
            .Options;

        return new ReviewDbContext(options);
    }

    private sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    private sealed class FakeTaskCatalog : ITaskCatalog
    {
        private readonly TaskSnapshot _snapshot;

        public FakeTaskCatalog()
            : this(Guid.NewGuid(), "Условие задачи", "Правильный ответ")
        {
        }

        public FakeTaskCatalog(Guid taskId, string statement, string correctAnswer)
        {
            TaskId = taskId;
            _snapshot = new TaskSnapshot(taskId, statement, correctAnswer, Now);
        }

        public Guid TaskId { get; }

        public Task<TaskSnapshot> GetPublishedTaskAsync(Guid taskId, CancellationToken cancellationToken)
        {
            if (taskId != TaskId)
            {
                throw new NotFoundException($"Задача '{taskId}' не найдена.");
            }

            return Task.FromResult(_snapshot);
        }
    }
}
