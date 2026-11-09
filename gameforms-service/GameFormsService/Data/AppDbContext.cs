using GameFormsService.Models;
using Microsoft.EntityFrameworkCore;

namespace GameFormsService.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Game> Games => Set<Game>();
    public DbSet<GameTask> Tasks => Set<GameTask>();
    public DbSet<FormSet> FormSets => Set<FormSet>();
    public DbSet<Form> Forms => Set<Form>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Game>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<GameTask>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<FormSet>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        modelBuilder.Entity<Form>().Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        modelBuilder.Entity<GameTask>().HasOne(x => x.Game).WithMany().HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<FormSet>().HasOne(x => x.Game).WithMany().HasForeignKey(x => x.GameId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Form>().HasOne(x => x.FormSet).WithMany().HasForeignKey(x => x.FormSetId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Form>().HasOne(x => x.Task).WithMany().HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<FormSet>().HasIndex(x => new { x.GameId, x.TeamId }).IsUnique();
        modelBuilder.Entity<Form>().HasIndex(x => new { x.FormSetId, x.TaskId }).IsUnique();
    }
}
