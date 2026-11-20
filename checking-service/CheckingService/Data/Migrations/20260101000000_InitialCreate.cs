using CheckingService.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace CheckingService.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20260101000000_InitialCreate")]
public class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("Judges", columns: table => new
        {
            Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            Name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
            Role = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
            IsAvailable = table.Column<bool>(type: "boolean", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Judges", x => x.Id));

        migrationBuilder.CreateTable("Submissions", columns: table => new
        {
            Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            TeamId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            GameId = table.Column<int>(type: "integer", nullable: false),
            FormId = table.Column<int>(type: "integer", nullable: false),
            TaskId = table.Column<int>(type: "integer", nullable: false),
            Answer = table.Column<string>(type: "text", nullable: false),
            PhotoPath = table.Column<string>(type: "text", nullable: true),
            PhotoFormId = table.Column<int>(type: "integer", nullable: true),
            TaskCondition = table.Column<string>(type: "text", nullable: false),
            CorrectAnswer = table.Column<string>(type: "text", nullable: false),
            Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_Submissions", x => x.Id));

        migrationBuilder.CreateTable("CheckRequests", columns: table => new
        {
            Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            SubmissionId = table.Column<int>(type: "integer", nullable: false),
            JudgeId = table.Column<int>(type: "integer", nullable: true),
            Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_CheckRequests", x => x.Id);
            table.ForeignKey("FK_CheckRequests_Judges_JudgeId", x => x.JudgeId, "Judges", "Id", onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_CheckRequests_Submissions_SubmissionId", x => x.SubmissionId, "Submissions", "Id", onDelete: ReferentialAction.Restrict);
        });

        migrationBuilder.CreateTable("CheckResults", columns: table => new
        {
            Id = table.Column<int>(type: "integer", nullable: false).Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
            CheckRequestId = table.Column<int>(type: "integer", nullable: false),
            IsCorrect = table.Column<bool>(type: "boolean", nullable: false),
            PointsAwarded = table.Column<int>(type: "integer", nullable: false),
            Comment = table.Column<string>(type: "text", nullable: false),
            CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_CheckResults", x => x.Id);
            table.ForeignKey("FK_CheckResults_CheckRequests_CheckRequestId", x => x.CheckRequestId, "CheckRequests", "Id", onDelete: ReferentialAction.Restrict);
        });

        migrationBuilder.CreateIndex("IX_CheckRequests_SubmissionId", "CheckRequests", "SubmissionId", unique: true);
        migrationBuilder.CreateIndex("IX_CheckRequests_JudgeId", "CheckRequests", "JudgeId", unique: true,
            filter: "\"JudgeId\" IS NOT NULL AND \"Status\" IN ('Assigned', 'Accepted', 'Escalated')");
        migrationBuilder.CreateIndex("IX_CheckResults_CheckRequestId", "CheckResults", "CheckRequestId", unique: true);
    }
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("CheckResults");
        migrationBuilder.DropTable("CheckRequests");
        migrationBuilder.DropTable("Judges");
        migrationBuilder.DropTable("Submissions");
    }
}
