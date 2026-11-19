using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace CheckingService.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261119000000_FormImports")]
public class FormImports : Migration
{
    protected override void Up(MigrationBuilder m)
    {
        m.AddColumn<string>("GameName", "Submissions", type: "character varying(200)", maxLength: 200, nullable: true);
        m.CreateIndex("IX_Submissions_FormId", "Submissions", "FormId", unique: true, filter: "\"GameName\" IS NOT NULL");
    }
    protected override void Down(MigrationBuilder m)
    {
        m.DropIndex("IX_Submissions_FormId", "Submissions");
        m.DropColumn("GameName", "Submissions");
    }
}
