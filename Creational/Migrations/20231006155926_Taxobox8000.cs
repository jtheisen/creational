using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Creational.Migrations
{
    public partial class Taxobox8000 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Taxobox",
                table: "Taxoboxes",
                type: "varchar(8000)",
                unicode: false,
                maxLength: 8000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(4000)",
                oldUnicode: false,
                oldMaxLength: 4000,
                oldNullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Taxobox",
                table: "Taxoboxes",
                type: "varchar(4000)",
                unicode: false,
                maxLength: 4000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(8000)",
                oldUnicode: false,
                oldMaxLength: 8000,
                oldNullable: true);
        }
    }
}
