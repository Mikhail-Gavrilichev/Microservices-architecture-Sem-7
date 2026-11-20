using GameFormsService.Data;
using GameFormsService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using TaskStatus = GameFormsService.Models.TaskStatus;

namespace GameFormsService.Data.Migrations;

[DbContext(typeof(AppDbContext))]
public class AppDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder.HasAnnotation("ProductVersion", "8.0.11");
        modelBuilder.HasAnnotation("Relational:MaxIdentifierLength", 63);
        modelBuilder.UseIdentityByDefaultColumns();

        modelBuilder.Entity<Game>(b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("integer").UseIdentityByDefaultColumn();
            b.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<string>("Description").IsRequired().HasColumnType("text");
            b.Property<string>("Rules").IsRequired().HasColumnType("text");
            b.Property<string>("FormRules").IsRequired().HasColumnType("text");
            b.Property<GameStatus>("Status").HasConversion<string>().IsRequired().HasMaxLength(20).HasColumnType("character varying(20)");
            b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
            b.HasKey("Id"); b.ToTable("Games");
        });
        modelBuilder.Entity<GameTask>(b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("integer").UseIdentityByDefaultColumn();
            b.Property<int>("GameId").HasColumnType("integer");
            b.Property<string>("Title").IsRequired().HasMaxLength(200).HasColumnType("character varying(200)");
            b.Property<string>("Condition").IsRequired().HasColumnType("text");
            b.Property<string>("CorrectAnswer").IsRequired().HasColumnType("text");
            b.Property<int>("Points").HasColumnType("integer");
            b.Property<TaskStatus>("Status").HasConversion<string>().IsRequired().HasMaxLength(20).HasColumnType("character varying(20)");
            b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
            b.HasKey("Id"); b.HasIndex("GameId"); b.ToTable("Tasks");
            b.HasOne<Game>("Game").WithMany().HasForeignKey("GameId").OnDelete(DeleteBehavior.Restrict).IsRequired();
        });
        modelBuilder.Entity<FormSet>(b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("integer").UseIdentityByDefaultColumn();
            b.Property<int>("GameId").HasColumnType("integer");
            b.Property<string>("TeamId").IsRequired().HasMaxLength(100).HasColumnType("character varying(100)");
            b.Property<FormSetStatus>("Status").HasConversion<string>().IsRequired().HasMaxLength(20).HasColumnType("character varying(20)");
            b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
            b.HasKey("Id"); b.HasIndex("GameId", "TeamId").IsUnique(); b.ToTable("FormSets");
            b.HasOne<Game>("Game").WithMany().HasForeignKey("GameId").OnDelete(DeleteBehavior.Restrict).IsRequired();
        });
        modelBuilder.Entity<Form>(b =>
        {
            b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("integer").UseIdentityByDefaultColumn();
            b.Property<int>("FormSetId").HasColumnType("integer");
            b.Property<int>("TaskId").HasColumnType("integer");
            b.Property<FormStatus>("Status").HasConversion<string>().IsRequired().HasMaxLength(20).HasColumnType("character varying(20)");
            b.Property<DateTime>("CreatedAt").HasColumnType("timestamp with time zone");
            b.HasKey("Id"); b.HasIndex("FormSetId", "TaskId").IsUnique(); b.HasIndex("TaskId"); b.ToTable("Forms");
            b.HasOne<FormSet>("FormSet").WithMany().HasForeignKey("FormSetId").OnDelete(DeleteBehavior.Restrict).IsRequired();
            b.HasOne<GameTask>("Task").WithMany().HasForeignKey("TaskId").OnDelete(DeleteBehavior.Restrict).IsRequired();
        });
    }
}
