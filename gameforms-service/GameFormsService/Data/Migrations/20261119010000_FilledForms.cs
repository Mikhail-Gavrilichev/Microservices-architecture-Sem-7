using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GameFormsService.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261119010000_FilledForms")]
public class FilledForms : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.AddColumn<string>("ParticipantAnswer", "Forms", "text", nullable: true);
        m.AddColumn<byte[]>("WorkPhoto", "Forms", "bytea", nullable: true);
        m.AddColumn<string>("PhotoExtension", "Forms", "text", nullable: true);
        m.AddColumn<bool>("Delivered", "Forms", "boolean", nullable: false, defaultValue: false);
    }
    protected override void Down(MigrationBuilder m)
    {
        foreach (var column in new[] { "ParticipantAnswer", "WorkPhoto", "PhotoExtension", "Delivered" })
            m.DropColumn(column, "Forms");
    }
}
