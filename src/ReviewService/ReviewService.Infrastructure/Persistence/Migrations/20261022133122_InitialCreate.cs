using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReviewService.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "review_requests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    solution_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_id = table.Column<Guid>(type: "uuid", nullable: false),
                    task_statement = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    correct_answer = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    participant_answer = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    team_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_reference_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    assigned_jury_member_id = table.Column<Guid>(type: "uuid", nullable: true),
                    refusal_reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    verdict = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    comment = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    queued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    accepted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    refused_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    transferred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_review_requests", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_review_requests_assigned_jury_member_id_status",
                table: "review_requests",
                columns: new[] { "assigned_jury_member_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_review_requests_status_queued_at",
                table: "review_requests",
                columns: new[] { "status", "queued_at" });

            migrationBuilder.CreateIndex(
                name: "IX_review_requests_task_id",
                table: "review_requests",
                column: "task_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "review_requests");
        }
    }
}
