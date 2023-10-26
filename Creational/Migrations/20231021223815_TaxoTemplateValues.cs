using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Creational.Migrations
{
    public partial class TaxoTemplateValues : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TaxoTemplateValues",
                columns: table => new
                {
                    Lang = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    Rank = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    Parent = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    PageTitle = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Name = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxoTemplateValues", x => new { x.Lang, x.Title });
                    table.ForeignKey(
                        name: "FK_TaxoTemplateValues_ParsingResults_Lang_Title",
                        columns: x => new { x.Lang, x.Title },
                        principalTable: "ParsingResults",
                        principalColumns: new[] { "Lang", "Title" },
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxoTemplateValues");
        }
    }
}
