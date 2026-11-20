using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GameFormsService.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261119020000_RemoveFilledWork")]
public class RemoveFilledWork : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        // Не уничтожать неотправленные работы предыдущей версии молча.
        m.Sql("""
            DO $$ BEGIN
                IF EXISTS (SELECT 1 FROM "Forms" WHERE "ParticipantAnswer" IS NOT NULL AND NOT "Delivered") THEN
                    RAISE EXCEPTION 'Pending filled forms: back up the database and resolve undelivered work before upgrading';
                END IF;
            END $$;
            """);
        foreach (var column in new[] { "ParticipantAnswer", "WorkPhoto", "PhotoExtension", "Delivered" })
            m.DropColumn(column, "Forms");
    }
    protected override void Down(MigrationBuilder m)
    {
        m.AddColumn<string>("ParticipantAnswer", "Forms", "text", nullable: true);
        m.AddColumn<byte[]>("WorkPhoto", "Forms", "bytea", nullable: true);
        m.AddColumn<string>("PhotoExtension", "Forms", "text", nullable: true);
        m.AddColumn<bool>("Delivered", "Forms", "boolean", nullable: false, defaultValue: false);
    }
}
