using CheckingService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace CheckingService.Data.Migrations;

[DbContext(typeof(AppDbContext))]
public class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder m)
    {
        m.HasAnnotation("ProductVersion", "8.0.11");
        m.UseIdentityByDefaultColumns();
        m.Entity<Submission>(b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("integer").UseIdentityByDefaultColumn();
            b.Property<string>("TeamId").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            b.Property<int>("GameId").HasColumnType("integer");
            b.Property<int>("FormId").HasColumnType("integer");
            b.Property<int>("TaskId").HasColumnType("integer");
            b.Property<string>("Answer").IsRequired().HasColumnType("text");
            b.Property<string>("PhotoPath").HasColumnType("text");
            b.Property<int?>("PhotoFormId").HasColumnType("integer");
            b.Property<string>("TaskCondition").IsRequired().HasColumnType("text");
            b.Property<string>("CorrectAnswer").IsRequired().HasColumnType("text");
            b.Property<SubmissionStatus>("Status").HasConversion<string>().IsRequired().HasMaxLength(30).HasColumnType("character varying(30)");
            b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
            b.HasKey("Id"); b.ToTable("Submissions");
        });
        m.Entity<Judge>(b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("integer").UseIdentityByDefaultColumn();
            b.Property<string>("Name").IsRequired().HasMaxLength(150).HasColumnType("character varying(150)");
            b.Property<JudgeRole>("Role").HasConversion<string>().IsRequired().HasMaxLength(30).HasColumnType("character varying(30)");
            b.Property<bool>("IsAvailable").HasColumnType("boolean");
            b.HasKey("Id"); b.ToTable("Judges");
        });
        m.Entity<CheckRequest>(b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("integer").UseIdentityByDefaultColumn();
            b.Property<int>("SubmissionId").HasColumnType("integer");
            b.Property<int?>("JudgeId").HasColumnType("integer");
            b.Property<CheckStatus>("Status").HasConversion<string>().IsRequired().HasMaxLength(30).HasColumnType("character varying(30)");
            b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
            b.Property<DateTime?>("AssignedAt").HasColumnType("timestamp with time zone");
            b.Property<DateTime?>("CompletedAt").HasColumnType("timestamp with time zone");
            b.HasKey("Id");
            b.HasIndex("SubmissionId").IsUnique();
            b.HasIndex("JudgeId").IsUnique().HasFilter("\"JudgeId\" IS NOT NULL AND \"Status\" IN ('Assigned', 'Accepted', 'Escalated')");
            b.ToTable("CheckRequests");
            b.HasOne<Submission>("Submission").WithMany().HasForeignKey("SubmissionId").OnDelete(DeleteBehavior.Restrict).IsRequired();
            b.HasOne<Judge>("Judge").WithMany().HasForeignKey("JudgeId").OnDelete(DeleteBehavior.Restrict);
        });
        m.Entity<CheckResult>(b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("integer").UseIdentityByDefaultColumn();
            b.Property<int>("CheckRequestId").HasColumnType("integer");
            b.Property<bool>("IsCorrect").HasColumnType("boolean");
            b.Property<int>("PointsAwarded").HasColumnType("integer");
            b.Property<string>("Comment").IsRequired().HasColumnType("text");
            b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
            b.HasKey("Id"); b.HasIndex("CheckRequestId").IsUnique(); b.ToTable("CheckResults");
            b.HasOne<CheckRequest>("CheckRequest").WithOne().HasForeignKey<CheckResult>("CheckRequestId").OnDelete(DeleteBehavior.Restrict).IsRequired();
        });
    }
}
