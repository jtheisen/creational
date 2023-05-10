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
                    Ns = table.Column<int>(type: "int", nullable: false),
                    Id = table.Column<int>(type: "int", nullable: false),
                    Issue = table.Column<string>(type: "varchar(8)", unicode: false, maxLength: 8, nullable: true)
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
                    Text = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false),
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
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PageContents");

            migrationBuilder.DropTable(
                name: "Pages");
        }
    }
}
