using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Creational.Migrations
{
    public partial class ImageCascades : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaxoboxImageEntries_ParsingResults_Lang_Title",
                table: "TaxoboxImageEntries");

            migrationBuilder.AddForeignKey(
                name: "FK_TaxoboxImageEntries_ParsingResults_Lang_Title",
                table: "TaxoboxImageEntries",
                columns: new[] { "Lang", "Title" },
                principalTable: "ParsingResults",
                principalColumns: new[] { "Lang", "Title" },
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaxoboxImageEntries_ParsingResults_Lang_Title",
                table: "TaxoboxImageEntries");

            migrationBuilder.AddForeignKey(
                name: "FK_TaxoboxImageEntries_ParsingResults_Lang_Title",
                table: "TaxoboxImageEntries",
                columns: new[] { "Lang", "Title" },
                principalTable: "ParsingResults",
                principalColumns: new[] { "Lang", "Title" });
        }
    }
}
