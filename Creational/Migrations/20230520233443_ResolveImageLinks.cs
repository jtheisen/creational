using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Creational.Migrations
{
    public partial class ResolveImageLinks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Filename",
                table: "ImageLinks",
                type: "varchar(2000)",
                unicode: false,
                maxLength: 2000,
                nullable: true,
                collation: "Latin1_General_BIN2",
                oldClrType: typeof(string),
                oldType: "varchar(2000)",
                oldUnicode: false,
                oldMaxLength: 2000,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "ResolvedImages",
                columns: table => new
                {
                    Filename = table.Column<string>(type: "varchar(2000)", unicode: false, maxLength: 2000, nullable: false, collation: "Latin1_General_BIN2"),
                    Uri = table.Column<string>(type: "varchar(2000)", unicode: false, maxLength: 2000, nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResolvedImages", x => x.Filename);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResolvedImages");

            migrationBuilder.AlterColumn<string>(
                name: "Filename",
                table: "ImageLinks",
                type: "varchar(2000)",
                unicode: false,
                maxLength: 2000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(2000)",
                oldUnicode: false,
                oldMaxLength: 2000,
                oldNullable: true,
                oldCollation: "Latin1_General_BIN2");
        }
    }
}
