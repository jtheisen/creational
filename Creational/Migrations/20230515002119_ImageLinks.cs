using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Creational.Migrations
{
    public partial class ImageLinks : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StepError",
                table: "Pages",
                type: "varchar(2000)",
                unicode: false,
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ImageLinks",
                columns: table => new
                {
                    Title = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    Filename = table.Column<string>(type: "varchar(2000)", unicode: false, maxLength: 2000, nullable: true),
                    Text = table.Column<string>(type: "varchar(2000)", unicode: false, maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageLinks", x => new { x.Title, x.Position });
                    table.ForeignKey(
                        name: "FK_ImageLinks_Pages_Title",
                        column: x => x.Title,
                        principalTable: "Pages",
                        principalColumn: "Title",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImageLinks");

            migrationBuilder.DropColumn(
                name: "StepError",
                table: "Pages");
        }
    }
}
