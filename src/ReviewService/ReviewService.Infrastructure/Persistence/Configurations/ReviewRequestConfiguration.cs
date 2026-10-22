using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReviewService.Domain.ReviewRequests;

namespace ReviewService.Infrastructure.Persistence.Configurations;

public sealed class ReviewRequestConfiguration : IEntityTypeConfiguration<ReviewRequest>
{
    public void Configure(EntityTypeBuilder<ReviewRequest> builder)
    {
        builder.ToTable("review_requests");

        builder.HasKey(request => request.Id);

        builder.Property(request => request.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(request => request.SolutionId)
            .HasColumnName("solution_id")
            .IsRequired();

        builder.Property(request => request.TaskId)
            .HasColumnName("task_id")
            .IsRequired();

        builder.Property(request => request.TaskStatement)
            .HasColumnName("task_statement")
            .HasMaxLength(ReviewRequest.TaskStatementMaxLength)
            .IsRequired();

        builder.Property(request => request.CorrectAnswer)
            .HasColumnName("correct_answer")
            .HasMaxLength(ReviewRequest.CorrectAnswerMaxLength)
            .IsRequired();

        builder.Property(request => request.ParticipantAnswer)
            .HasColumnName("participant_answer")
            .HasMaxLength(ReviewRequest.ParticipantAnswerMaxLength)
            .IsRequired();

        builder.Property(request => request.TeamId)
            .HasColumnName("team_id")
            .IsRequired();

        builder.Property(request => request.Source)
            .HasColumnName("source")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(request => request.SourceReferenceId)
            .HasColumnName("source_reference_id");

        builder.Property(request => request.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(request => request.AssignedJuryMemberId)
            .HasColumnName("assigned_jury_member_id");

        builder.Property(request => request.RefusalReason)
            .HasColumnName("refusal_reason")
            .HasMaxLength(ReviewRequest.RefusalReasonMaxLength);

        builder.Property(request => request.Verdict)
            .HasColumnName("verdict")
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(request => request.Comment)
            .HasColumnName("comment")
            .HasMaxLength(ReviewRequest.CommentMaxLength);

        builder.Property(request => request.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(request => request.UpdatedAt).HasColumnName("updated_at").IsRequired();
        builder.Property(request => request.QueuedAt).HasColumnName("queued_at");
        builder.Property(request => request.AssignedAt).HasColumnName("assigned_at");
        builder.Property(request => request.AcceptedAt).HasColumnName("accepted_at");
        builder.Property(request => request.RefusedAt).HasColumnName("refused_at");
        builder.Property(request => request.TransferredAt).HasColumnName("transferred_at");
        builder.Property(request => request.CompletedAt).HasColumnName("completed_at");

        builder.Ignore(request => request.DomainEvents);

        builder.HasIndex(request => new { request.Status, request.QueuedAt });
        builder.HasIndex(request => new { request.AssignedJuryMemberId, request.Status });
        builder.HasIndex(request => request.TaskId);
    }
}
