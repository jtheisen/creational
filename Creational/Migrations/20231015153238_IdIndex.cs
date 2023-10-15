using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Creational.Migrations
{
    public partial class IdIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Pages_Lang_Id",
                table: "Pages",
                columns: new[] { "Lang", "Id" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pages_Lang_Id",
                table: "Pages");
        }
    }
}
