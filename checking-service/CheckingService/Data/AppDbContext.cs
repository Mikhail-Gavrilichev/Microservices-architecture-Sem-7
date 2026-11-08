using CheckingService.Models;
using Microsoft.EntityFrameworkCore;

namespace CheckingService.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Submission> Submissions => Set<Submission>();
    public DbSet<CheckRequest> CheckRequests => Set<CheckRequest>();
    public DbSet<Judge> Judges => Set<Judge>();
    public DbSet<CheckResult> CheckResults => Set<CheckResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Submission>().Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        modelBuilder.Entity<CheckRequest>().Property(x => x.Status).HasConversion<string>().HasMaxLength(30);
        modelBuilder.Entity<Judge>().Property(x => x.Role).HasConversion<string>().HasMaxLength(30);

        modelBuilder.Entity<CheckRequest>().HasOne(x => x.Submission).WithMany()
            .HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CheckRequest>().HasOne(x => x.Judge).WithMany()
            .HasForeignKey(x => x.JudgeId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CheckResult>().HasOne(x => x.CheckRequest).WithOne()
            .HasForeignKey<CheckResult>(x => x.CheckRequestId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CheckRequest>().HasIndex(x => x.SubmissionId).IsUnique();
        modelBuilder.Entity<CheckRequest>().HasIndex(x => x.JudgeId).IsUnique()
            .HasFilter("\"JudgeId\" IS NOT NULL AND \"Status\" IN ('Assigned', 'Accepted', 'Escalated')");
        modelBuilder.Entity<CheckResult>().HasIndex(x => x.CheckRequestId).IsUnique();
    }
}
