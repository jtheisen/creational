using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Creational.Migrations
{
    public partial class Parsing : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Step",
                table: "Pages",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ParsingResults",
                columns: table => new
                {
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Sha1 = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Redirection = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Exception = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    WithTaxobox = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParsingResults", x => x.Title);
                    table.ForeignKey(
                        name: "FK_ParsingResults_Pages_Title",
                        column: x => x.Title,
                        principalTable: "Pages",
                        principalColumn: "Title",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxoboxEntries",
                columns: table => new
                {
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Key = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Value = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxoboxEntries", x => new { x.Title, x.Key });
                    table.ForeignKey(
                        name: "FK_TaxoboxEntries_ParsingResults_Title",
                        column: x => x.Title,
                        principalTable: "ParsingResults",
                        principalColumn: "Title",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pages_Step",
                table: "Pages",
                column: "Step");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxoboxEntries");

            migrationBuilder.DropTable(
                name: "ParsingResults");

            migrationBuilder.DropIndex(
                name: "IX_Pages_Step",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "Step",
                table: "Pages");
        }
    }
}
