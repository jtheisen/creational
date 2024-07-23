using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Creational.Migrations
{
    public partial class ExtinctSituationAndRefactorings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaxoboxEntries_ParsingResults_Lang_Title",
                table: "TaxoboxEntries");

            migrationBuilder.DropTable(
                name: "TaxonomyEntry");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaxoboxEntries",
                table: "TaxoboxEntries");

            migrationBuilder.DropColumn(
                name: "Genus",
                table: "ParsingResults");

            migrationBuilder.DropColumn(
                name: "HasTruncationIssue",
                table: "ParsingResults");

            migrationBuilder.DropColumn(
                name: "Parent",
                table: "ParsingResults");

            migrationBuilder.DropColumn(
                name: "Species",
                table: "ParsingResults");

            migrationBuilder.DropColumn(
                name: "Taxon",
                table: "ParsingResults");

            migrationBuilder.DropColumn(
                name: "WithTaxobox",
                table: "ParsingResults");

            migrationBuilder.RenameTable(
                name: "TaxoboxEntries",
                newName: "TaxoboxEntry");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaxoboxEntry",
                table: "TaxoboxEntry",
                columns: new[] { "Lang", "Title", "Key" });

            migrationBuilder.CreateTable(
                name: "SpeciesValues",
                columns: table => new
                {
                    Lang = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    ExtantSituation = table.Column<int>(type: "int", nullable: false),
                    Taxon = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Genus = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true),
                    Species = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpeciesValues", x => new { x.Lang, x.Title });
                    table.ForeignKey(
                        name: "FK_SpeciesValues_ParsingResults_Lang_Title",
                        columns: x => new { x.Lang, x.Title },
                        principalTable: "ParsingResults",
                        principalColumns: new[] { "Lang", "Title" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_TaxoboxEntry_ParsingResults_Lang_Title",
                table: "TaxoboxEntry",
                columns: new[] { "Lang", "Title" },
                principalTable: "ParsingResults",
                principalColumns: new[] { "Lang", "Title" },
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaxoboxEntry_ParsingResults_Lang_Title",
                table: "TaxoboxEntry");

            migrationBuilder.DropTable(
                name: "SpeciesValues");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaxoboxEntry",
                table: "TaxoboxEntry");

            migrationBuilder.RenameTable(
                name: "TaxoboxEntry",
                newName: "TaxoboxEntries");

            migrationBuilder.AddColumn<string>(
                name: "Genus",
                table: "ParsingResults",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasTruncationIssue",
                table: "ParsingResults",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "Parent",
                table: "ParsingResults",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Species",
                table: "ParsingResults",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Taxon",
                table: "ParsingResults",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "WithTaxobox",
                table: "ParsingResults",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaxoboxEntries",
                table: "TaxoboxEntries",
                columns: new[] { "Lang", "Title", "Key" });

            migrationBuilder.CreateTable(
                name: "TaxonomyEntry",
                columns: table => new
                {
                    Lang = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: false),
                    Title = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    No = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    NameLocal = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true),
                    Rank = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxonomyEntry", x => new { x.Lang, x.Title, x.No });
                    table.ForeignKey(
                        name: "FK_TaxonomyEntry_ParsingResults_Lang_Title",
                        columns: x => new { x.Lang, x.Title },
                        principalTable: "ParsingResults",
                        principalColumns: new[] { "Lang", "Title" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyEntry_Lang_Name_Rank_Title",
                table: "TaxonomyEntry",
                columns: new[] { "Lang", "Name", "Rank", "Title" })
                .Annotation("SqlServer:Include", new[] { "NameLocal" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyEntry_Lang_NameLocal_Rank_Title",
                table: "TaxonomyEntry",
                columns: new[] { "Lang", "NameLocal", "Rank", "Title" })
                .Annotation("SqlServer:Include", new[] { "Name" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyEntry_Lang_Rank_Title",
                table: "TaxonomyEntry",
                columns: new[] { "Lang", "Rank", "Title" })
                .Annotation("SqlServer:Include", new[] { "Name", "NameLocal" });

            migrationBuilder.AddForeignKey(
                name: "FK_TaxoboxEntries_ParsingResults_Lang_Title",
                table: "TaxoboxEntries",
                columns: new[] { "Lang", "Title" },
                principalTable: "ParsingResults",
                principalColumns: new[] { "Lang", "Title" },
                onDelete: ReferentialAction.Cascade);
        }
    }
}
