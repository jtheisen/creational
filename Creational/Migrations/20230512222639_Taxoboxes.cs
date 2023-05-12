using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Creational.Migrations
{
    public partial class Taxoboxes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Pages_Step",
                table: "Pages");

            migrationBuilder.AddColumn<bool>(
                name: "HasTruncationIssue",
                table: "ParsingResults",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Taxoboxes",
                columns: table => new
                {
                    Title = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    Sha1 = table.Column<string>(type: "varchar(40)", unicode: false, maxLength: 40, nullable: true),
                    Taxobox = table.Column<string>(type: "varchar(4000)", unicode: false, maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Taxoboxes", x => x.Title);
                    table.ForeignKey(
                        name: "FK_Taxoboxes_Pages_Title",
                        column: x => x.Title,
                        principalTable: "Pages",
                        principalColumn: "Title",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxonomyEntry",
                columns: table => new
                {
                    Title = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    No = table.Column<int>(type: "int", nullable: false),
                    Rank = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    Name = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    NameDe = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxonomyEntry", x => new { x.Title, x.No });
                    table.ForeignKey(
                        name: "FK_TaxonomyEntry_ParsingResults_Title",
                        column: x => x.Title,
                        principalTable: "ParsingResults",
                        principalColumn: "Title",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TaxonomyRelations",
                columns: table => new
                {
                    Ancestor = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    Descendant = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    No = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxonomyRelations", x => new { x.Ancestor, x.Descendant });
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
                columns: new[] { "NameDe", "Rank", "Title" })
                .Annotation("SqlServer:Include", new[] { "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyEntry_Rank_Title",
                table: "TaxonomyEntry",
                columns: new[] { "Rank", "Title" })
                .Annotation("SqlServer:Include", new[] { "Name", "NameDe" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyRelations_Descendant_Ancestor",
                table: "TaxonomyRelations",
                columns: new[] { "Descendant", "Ancestor" })
                .Annotation("SqlServer:Include", new[] { "No" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Taxoboxes");

            migrationBuilder.DropTable(
                name: "TaxonomyEntry");

            migrationBuilder.DropTable(
                name: "TaxonomyRelations");

            migrationBuilder.DropIndex(
                name: "IX_Pages_Step_Type",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "HasTruncationIssue",
                table: "ParsingResults");

            migrationBuilder.CreateIndex(
                name: "IX_Pages_Step",
                table: "Pages",
                column: "Step");
        }
    }
}
