using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Creational.Migrations
{
    public partial class NewImageEntries : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Genus",
                table: "ParsingResults",
                type: "varchar(60)",
                unicode: false,
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageSituation",
                table: "ParsingResults",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Species",
                table: "ParsingResults",
                type: "varchar(60)",
                unicode: false,
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Taxon",
                table: "ParsingResults",
                type: "varchar(60)",
                unicode: false,
                maxLength: 60,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TaxoboxImageEntries",
                columns: table => new
                {
                    Lang = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    Filename = table.Column<string>(type: "varchar(2000)", unicode: false, maxLength: 2000, nullable: true, collation: "Latin1_General_BIN2")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxoboxImageEntries", x => new { x.Lang, x.Title });
                    table.ForeignKey(
                        name: "FK_TaxoboxImageEntries_ParsingResults_Lang_Title",
                        columns: x => new { x.Lang, x.Title },
                        principalTable: "ParsingResults",
                        principalColumns: new[] { "Lang", "Title" });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxoboxImageEntries");

            migrationBuilder.DropColumn(
                name: "Genus",
                table: "ParsingResults");

            migrationBuilder.DropColumn(
                name: "ImageSituation",
                table: "ParsingResults");

            migrationBuilder.DropColumn(
                name: "Species",
                table: "ParsingResults");

            migrationBuilder.DropColumn(
                name: "Taxon",
                table: "ParsingResults");
        }
    }
}
