using ReviewService.Domain.Common;
using ReviewService.Domain.ReviewRequests;
using ReviewService.Domain.ReviewRequests.Events;

namespace ReviewService.Domain.Tests.ReviewRequests;

public sealed class ReviewRequestTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_starts_as_created_and_raises_event()
    {
        var request = CreateRequest();

        Assert.Equal(ReviewStatus.Created, request.Status);
        Assert.Equal("Условие задачи", request.TaskStatement);
        Assert.Equal("Правильный ответ", request.CorrectAnswer);
        Assert.Equal("Ответ участника", request.ParticipantAnswer);
        Assert.NotEqual(Guid.Empty, request.TeamId);
        Assert.IsType<ReviewRequestCreatedDomainEvent>(Assert.Single(request.DomainEvents));
    }

    [Fact]
    public void Create_appeal_without_source_reference_throws()
    {
        Assert.Throws<DomainException>(() => ReviewRequest.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Условие",
            "Ответ",
            "Ответ участника",
            Guid.NewGuid(),
            ReviewSource.Appeal,
            sourceReferenceId: null,
            Now));
    }

    [Fact]
    public void Enqueue_moves_created_request_to_queue()
    {
        var request = CreateRequest();

        request.Enqueue(Now.AddMinutes(1));

        Assert.Equal(ReviewStatus.Queued, request.Status);
        Assert.Equal(Now.AddMinutes(1), request.QueuedAt);
        Assert.Single(request.DomainEvents.OfType<ReviewRequestQueuedDomainEvent>());
    }

    [Fact]
    public void Assign_requires_queued_status()
    {
        var request = CreateRequest();

        Assert.Throws<DomainException>(() => request.Assign(Guid.NewGuid(), Now));
    }

    [Fact]
    public void Assign_from_queue_sets_jury_member()
    {
        var request = CreateRequest();
        var juryMemberId = Guid.NewGuid();
        request.Enqueue(Now);

        request.Assign(juryMemberId, Now.AddMinutes(1));

        Assert.Equal(ReviewStatus.Assigned, request.Status);
        Assert.Equal(juryMemberId, request.AssignedJuryMemberId);
        Assert.Equal(Now.AddMinutes(1), request.AssignedAt);
    }

    [Fact]
    public void Accept_by_another_jury_member_throws()
    {
        var request = CreateRequest();
        request.Enqueue(Now);
        request.Assign(Guid.NewGuid(), Now);

        Assert.Throws<DomainException>(() => request.Accept(Guid.NewGuid(), Now));
    }

    [Fact]
    public void Accept_by_assigned_jury_member_sets_accepted()
    {
        var request = CreateRequest();
        var juryMemberId = Guid.NewGuid();
        request.Enqueue(Now);
        request.Assign(juryMemberId, Now);

        request.Accept(juryMemberId, Now.AddMinutes(1));

        Assert.Equal(ReviewStatus.Accepted, request.Status);
        Assert.Equal(Now.AddMinutes(1), request.AcceptedAt);
    }

    [Fact]
    public void Refuse_then_transfer_to_senior_moves_work_to_senior()
    {
        var request = CreateRequest();
        var juryMemberId = Guid.NewGuid();
        var seniorId = Guid.NewGuid();
        request.Enqueue(Now);
        request.Assign(juryMemberId, Now);

        request.Refuse(juryMemberId, "Конфликт интересов", Now.AddMinutes(1));
        request.TransferToSenior(seniorId, Now.AddMinutes(2));

        Assert.Equal(ReviewStatus.TransferredToSenior, request.Status);
        Assert.Equal(seniorId, request.AssignedJuryMemberId);
        Assert.Equal("Конфликт интересов", request.RefusalReason);
        Assert.Equal(Now.AddMinutes(2), request.TransferredAt);
        Assert.Single(request.DomainEvents.OfType<ReviewTransferredToSeniorDomainEvent>());
    }

    [Fact]
    public void Transfer_to_same_jury_member_throws()
    {
        var request = CreateRequest();
        var juryMemberId = Guid.NewGuid();
        request.Enqueue(Now);
        request.Assign(juryMemberId, Now);
        request.Refuse(juryMemberId, "Причина", Now);

        Assert.Throws<DomainException>(() => request.TransferToSenior(juryMemberId, Now));
    }

    [Fact]
    public void Transfer_requires_refused_status()
    {
        var request = CreateRequest();
        request.Enqueue(Now);
        request.Assign(Guid.NewGuid(), Now);

        Assert.Throws<DomainException>(() =>
            request.TransferToSenior(Guid.NewGuid(), Now));
    }

    [Fact]
    public void Complete_by_assigned_jury_member_records_verdict()
    {
        var request = CreateRequest();
        var juryMemberId = Guid.NewGuid();
        request.Enqueue(Now);
        request.Assign(juryMemberId, Now);

        request.Complete(juryMemberId, ReviewVerdict.Correct, "  Верно  ", Now.AddMinutes(3));

        Assert.Equal(ReviewStatus.Completed, request.Status);
        Assert.Equal(ReviewVerdict.Correct, request.Verdict);
        Assert.Equal("Верно", request.Comment);
        Assert.Equal(Now.AddMinutes(3), request.CompletedAt);
        Assert.Single(request.DomainEvents.OfType<ReviewCompletedDomainEvent>());
    }

    [Fact]
    public void Complete_by_wrong_jury_member_throws()
    {
        var request = CreateRequest();
        request.Enqueue(Now);
        request.Assign(Guid.NewGuid(), Now);

        Assert.Throws<DomainException>(() =>
            request.Complete(Guid.NewGuid(), ReviewVerdict.Correct, null, Now));
    }

    [Fact]
    public void Complete_twice_throws()
    {
        var request = CreateRequest();
        var juryMemberId = Guid.NewGuid();
        request.Enqueue(Now);
        request.Assign(juryMemberId, Now);
        request.Complete(juryMemberId, ReviewVerdict.Correct, null, Now);

        Assert.Throws<DomainException>(() =>
            request.Complete(juryMemberId, ReviewVerdict.Incorrect, null, Now));
    }

    private static ReviewRequest CreateRequest() => ReviewRequest.Create(
        solutionId: Guid.NewGuid(),
        taskId: Guid.NewGuid(),
        taskStatement: "Условие задачи",
        correctAnswer: "Правильный ответ",
        participantAnswer: "Ответ участника",
        teamId: Guid.NewGuid(),
        source: ReviewSource.Solution,
        sourceReferenceId: null,
        now: Now);
}
