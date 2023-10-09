using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Creational.Migrations
{
    public partial class StepErrorIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Pages_Lang_StepError",
                table: "Pages",
                columns: new[] { "Lang", "StepError" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pages_Lang_StepError",
                table: "Pages");
        }
    }
}
