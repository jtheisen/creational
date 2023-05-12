using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Creational.Migrations
{
    public partial class Taxonomy : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pages_Step",
                table: "Pages");

            migrationBuilder.CreateTable(
                name: "TaxonomyEntry",
                columns: table => new
                {
                    Title = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    No = table.Column<int>(type: "int", nullable: false),
                    Rank = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    NameDe = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxonomyEntry", x => new { x.Title, x.Name });
                    table.ForeignKey(
                        name: "FK_TaxonomyEntry_ParsingResults_Title",
                        column: x => x.Title,
                        principalTable: "ParsingResults",
                        principalColumn: "Title",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Pages_Step_Type",
                table: "Pages",
                columns: new[] { "Step", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyEntry_Name_Rank_Title",
                table: "TaxonomyEntry",
                columns: new[] { "Name", "Rank", "Title" })
                .Annotation("SqlServer:Include", new[] { "NameDe" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyEntry_NameDe_Rank_Title",
                table: "TaxonomyEntry",
                columns: new[] { "NameDe", "Rank", "Title" },
                unique: true,
                filter: "[NameDe] IS NOT NULL AND [Rank] IS NOT NULL")
                .Annotation("SqlServer:Include", new[] { "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyEntry_Rank_Title",
                table: "TaxonomyEntry",
                columns: new[] { "Rank", "Title" })
                .Annotation("SqlServer:Include", new[] { "Name", "NameDe" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TaxonomyEntry");

            migrationBuilder.DropIndex(
                name: "IX_Pages_Step_Type",
                table: "Pages");

            migrationBuilder.CreateIndex(
                name: "IX_Pages_Step",
                table: "Pages",
                column: "Step");
        }
    }
}
