using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Creational.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Pages",
                columns: table => new
                {
                    Title = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false),
                    Ns = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
                    Issue = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: true),
                    Step = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pages", x => x.Title);
                });

            migrationBuilder.CreateTable(
                name: "PageContents",
                columns: table => new
                {
                    Title = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    RedirectTitle = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Text = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Model = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Format = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Sha1 = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageContents", x => x.Title);
                    table.ForeignKey(
                        name: "FK_PageContents_Pages_Title",
                        column: x => x.Title,
                        principalTable: "Pages",
                        principalColumn: "Title",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParsingResults",
                columns: table => new
                {
                    Title = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    Sha1 = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: true),
                    Redirection = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Exception = table.Column<string>(type: "varchar(1000)", unicode: false, maxLength: 1000, nullable: true),
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
                    Title = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    Key = table.Column<string>(type: "varchar(60)", unicode: false, maxLength: 60, nullable: false),
                    Value = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true)
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
                name: "PageContents");

            migrationBuilder.DropTable(
                name: "TaxoboxEntries");

            migrationBuilder.DropTable(
                name: "ParsingResults");

            migrationBuilder.DropTable(
                name: "Pages");
        }
    }
}
