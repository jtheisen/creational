using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Creational.Migrations
{
    public partial class FixImageDataKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ImageData",
                table: "ImageData");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImageData",
                table: "ImageData",
                columns: new[] { "Filename", "Kind" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_ImageData",
                table: "ImageData");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImageData",
                table: "ImageData",
                column: "Filename");
        }
    }
}
