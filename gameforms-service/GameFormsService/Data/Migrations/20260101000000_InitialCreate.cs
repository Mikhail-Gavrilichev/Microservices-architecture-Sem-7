using GameFormsService.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace GameFormsService.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260101000000_InitialCreate")]
public class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(name: "Games", columns: table => new
        {
            Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
            Description = table.Column<string>(type: "text", nullable: false),
            Rules = table.Column<string>(type: "text", nullable: false),
            FormRules = table.Column<string>(type: "text", nullable: false),
            Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Games", x => x.Id));

        migrationBuilder.CreateTable(name: "Tasks", columns: table => new
        {
            Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            GameId = table.Column<int>(type: "integer", nullable: false),
            Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
            Condition = table.Column<string>(type: "text", nullable: false),
            CorrectAnswer = table.Column<string>(type: "text", nullable: false),
            Points = table.Column<int>(type: "integer", nullable: false),
            Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_Tasks", x => x.Id);
            table.ForeignKey("FK_Tasks_Games_GameId", x => x.GameId, "Games", "Id", onDelete: ReferentialAction.Restrict);
        });

        migrationBuilder.CreateTable(name: "FormSets", columns: table => new
        {
            Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            GameId = table.Column<int>(type: "integer", nullable: false),
            TeamId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_FormSets", x => x.Id);
            table.ForeignKey("FK_FormSets_Games_GameId", x => x.GameId, "Games", "Id", onDelete: ReferentialAction.Restrict);
        });

        migrationBuilder.CreateTable(name: "Forms", columns: table => new
        {
            Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            FormSetId = table.Column<int>(type: "integer", nullable: false),
            TaskId = table.Column<int>(type: "integer", nullable: false),
            Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_Forms", x => x.Id);
            table.ForeignKey("FK_Forms_FormSets_FormSetId", x => x.FormSetId, "FormSets", "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_Forms_Tasks_TaskId", x => x.TaskId, "Tasks", "Id", onDelete: ReferentialAction.Restrict);
        });

        migrationBuilder.CreateIndex("IX_Tasks_GameId", "Tasks", "GameId");
        migrationBuilder.CreateIndex("IX_FormSets_GameId_TeamId", "FormSets", new[] { "GameId", "TeamId" }, unique: true);
        migrationBuilder.CreateIndex("IX_Forms_FormSetId_TaskId", "Forms", new[] { "FormSetId", "TaskId" }, unique: true);
        migrationBuilder.CreateIndex("IX_Forms_TaskId", "Forms", "TaskId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("Forms");
        migrationBuilder.DropTable("FormSets");
        migrationBuilder.DropTable("Tasks");
        migrationBuilder.DropTable("Games");
    }
}
