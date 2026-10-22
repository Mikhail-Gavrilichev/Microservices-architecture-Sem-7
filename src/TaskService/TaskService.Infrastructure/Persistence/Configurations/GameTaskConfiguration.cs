using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskService.Domain.GameTasks;

namespace TaskService.Infrastructure.Persistence.Configurations;

public sealed class GameTaskConfiguration : IEntityTypeConfiguration<GameTask>
{
    public void Configure(EntityTypeBuilder<GameTask> builder)
    {
        builder.ToTable("game_tasks");

        builder.HasKey(task => task.Id);

        builder.Property(task => task.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(task => task.Title)
            .HasColumnName("title")
            .HasMaxLength(GameTask.TitleMaxLength)
            .IsRequired();

        builder.Property(task => task.Statement)
            .HasColumnName("statement")
            .HasMaxLength(GameTask.StatementMaxLength)
            .IsRequired();

        builder.Property(task => task.CorrectAnswer)
            .HasColumnName("correct_answer")
            .HasMaxLength(GameTask.CorrectAnswerMaxLength)
            .IsRequired();

        builder.Property(task => task.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(task => task.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(task => task.UpdatedAt)
            .HasColumnName("updated_at")
            .IsRequired();

        builder.Property(task => task.PublishedAt)
            .HasColumnName("published_at");

        builder.Ignore(task => task.DomainEvents);

        builder.HasIndex(task => task.Status);
        builder.HasIndex(task => new { task.Status, task.CreatedAt });
    }
}
