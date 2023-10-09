using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Creational.Migrations
{
    public partial class Language : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImageLinks_Pages_Title",
                table: "ImageLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_PageContents_Pages_Title",
                table: "PageContents");

            migrationBuilder.DropForeignKey(
                name: "FK_ParsingResults_Pages_Title",
                table: "ParsingResults");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxoboxEntries_ParsingResults_Title",
                table: "TaxoboxEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_Taxoboxes_Pages_Title",
                table: "Taxoboxes");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxonomyEntry_ParsingResults_Title",
                table: "TaxonomyEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxonomyRelations_Pages_Ancestor",
                table: "TaxonomyRelations");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxonomyRelations_Pages_Descendant",
                table: "TaxonomyRelations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaxonomyRelations",
                table: "TaxonomyRelations");

            migrationBuilder.DropIndex(
                name: "IX_TaxonomyRelations_Descendant_Ancestor",
                table: "TaxonomyRelations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaxonomyEntry",
                table: "TaxonomyEntry");

            migrationBuilder.DropIndex(
                name: "IX_TaxonomyEntry_Name_Rank_Title",
                table: "TaxonomyEntry");

            migrationBuilder.DropIndex(
                name: "IX_TaxonomyEntry_NameDe_Rank_Title",
                table: "TaxonomyEntry");

            migrationBuilder.DropIndex(
                name: "IX_TaxonomyEntry_Rank_Title",
                table: "TaxonomyEntry");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Taxoboxes",
                table: "Taxoboxes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaxoboxEntries",
                table: "TaxoboxEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ParsingResults",
                table: "ParsingResults");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Pages",
                table: "Pages");

            migrationBuilder.DropIndex(
                name: "IX_Pages_Step_Type",
                table: "Pages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PageContents",
                table: "PageContents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ImageLinks",
                table: "ImageLinks");

            migrationBuilder.RenameColumn(
                name: "NameDe",
                table: "TaxonomyEntry",
                newName: "NameLocal");

            migrationBuilder.AddColumn<string>(
                name: "Lang",
                table: "TaxonomyRelations",
                type: "varchar(2)",
                unicode: false,
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Lang",
                table: "TaxonomyEntry",
                type: "varchar(2)",
                unicode: false,
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Lang",
                table: "TaxoboxImages",
                type: "varchar(2)",
                unicode: false,
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Lang",
                table: "Taxoboxes",
                type: "varchar(2)",
                unicode: false,
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Lang",
                table: "TaxoboxEntries",
                type: "varchar(2)",
                unicode: false,
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Lang",
                table: "ParsingResults",
                type: "varchar(2)",
                unicode: false,
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Lang",
                table: "Pages",
                type: "varchar(2)",
                unicode: false,
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Lang",
                table: "PageContents",
                type: "varchar(2)",
                unicode: false,
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Lang",
                table: "ImageLinks",
                type: "varchar(2)",
                unicode: false,
                maxLength: 2,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaxonomyRelations",
                table: "TaxonomyRelations",
                columns: new[] { "Lang", "Ancestor", "Descendant" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaxonomyEntry",
                table: "TaxonomyEntry",
                columns: new[] { "Lang", "Title", "No" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Taxoboxes",
                table: "Taxoboxes",
                columns: new[] { "Lang", "Title" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaxoboxEntries",
                table: "TaxoboxEntries",
                columns: new[] { "Lang", "Title", "Key" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ParsingResults",
                table: "ParsingResults",
                columns: new[] { "Lang", "Title" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Pages",
                table: "Pages",
                columns: new[] { "Lang", "Title" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_PageContents",
                table: "PageContents",
                columns: new[] { "Lang", "Title" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImageLinks",
                table: "ImageLinks",
                columns: new[] { "Lang", "Title", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyRelations_Lang_Descendant_Ancestor",
                table: "TaxonomyRelations",
                columns: new[] { "Lang", "Descendant", "Ancestor" })
                .Annotation("SqlServer:Include", new[] { "No" });

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

            migrationBuilder.CreateIndex(
                name: "IX_Pages_Lang_Step_Type",
                table: "Pages",
                columns: new[] { "Lang", "Step", "Type" });

            migrationBuilder.AddForeignKey(
                name: "FK_ImageLinks_Pages_Lang_Title",
                table: "ImageLinks",
                columns: new[] { "Lang", "Title" },
                principalTable: "Pages",
                principalColumns: new[] { "Lang", "Title" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PageContents_Pages_Lang_Title",
                table: "PageContents",
                columns: new[] { "Lang", "Title" },
                principalTable: "Pages",
                principalColumns: new[] { "Lang", "Title" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ParsingResults_Pages_Lang_Title",
                table: "ParsingResults",
                columns: new[] { "Lang", "Title" },
                principalTable: "Pages",
                principalColumns: new[] { "Lang", "Title" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaxoboxEntries_ParsingResults_Lang_Title",
                table: "TaxoboxEntries",
                columns: new[] { "Lang", "Title" },
                principalTable: "ParsingResults",
                principalColumns: new[] { "Lang", "Title" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Taxoboxes_Pages_Lang_Title",
                table: "Taxoboxes",
                columns: new[] { "Lang", "Title" },
                principalTable: "Pages",
                principalColumns: new[] { "Lang", "Title" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaxonomyEntry_ParsingResults_Lang_Title",
                table: "TaxonomyEntry",
                columns: new[] { "Lang", "Title" },
                principalTable: "ParsingResults",
                principalColumns: new[] { "Lang", "Title" },
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaxonomyRelations_Pages_Lang_Ancestor",
                table: "TaxonomyRelations",
                columns: new[] { "Lang", "Ancestor" },
                principalTable: "Pages",
                principalColumns: new[] { "Lang", "Title" });

            migrationBuilder.AddForeignKey(
                name: "FK_TaxonomyRelations_Pages_Lang_Descendant",
                table: "TaxonomyRelations",
                columns: new[] { "Lang", "Descendant" },
                principalTable: "Pages",
                principalColumns: new[] { "Lang", "Title" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ImageLinks_Pages_Lang_Title",
                table: "ImageLinks");

            migrationBuilder.DropForeignKey(
                name: "FK_PageContents_Pages_Lang_Title",
                table: "PageContents");

            migrationBuilder.DropForeignKey(
                name: "FK_ParsingResults_Pages_Lang_Title",
                table: "ParsingResults");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxoboxEntries_ParsingResults_Lang_Title",
                table: "TaxoboxEntries");

            migrationBuilder.DropForeignKey(
                name: "FK_Taxoboxes_Pages_Lang_Title",
                table: "Taxoboxes");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxonomyEntry_ParsingResults_Lang_Title",
                table: "TaxonomyEntry");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxonomyRelations_Pages_Lang_Ancestor",
                table: "TaxonomyRelations");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxonomyRelations_Pages_Lang_Descendant",
                table: "TaxonomyRelations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaxonomyRelations",
                table: "TaxonomyRelations");

            migrationBuilder.DropIndex(
                name: "IX_TaxonomyRelations_Lang_Descendant_Ancestor",
                table: "TaxonomyRelations");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaxonomyEntry",
                table: "TaxonomyEntry");

            migrationBuilder.DropIndex(
                name: "IX_TaxonomyEntry_Lang_Name_Rank_Title",
                table: "TaxonomyEntry");

            migrationBuilder.DropIndex(
                name: "IX_TaxonomyEntry_Lang_NameLocal_Rank_Title",
                table: "TaxonomyEntry");

            migrationBuilder.DropIndex(
                name: "IX_TaxonomyEntry_Lang_Rank_Title",
                table: "TaxonomyEntry");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Taxoboxes",
                table: "Taxoboxes");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TaxoboxEntries",
                table: "TaxoboxEntries");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ParsingResults",
                table: "ParsingResults");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Pages",
                table: "Pages");

            migrationBuilder.DropIndex(
                name: "IX_Pages_Lang_Step_Type",
                table: "Pages");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PageContents",
                table: "PageContents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ImageLinks",
                table: "ImageLinks");

            migrationBuilder.DropColumn(
                name: "Lang",
                table: "TaxonomyRelations");

            migrationBuilder.DropColumn(
                name: "Lang",
                table: "TaxonomyEntry");

            migrationBuilder.DropColumn(
                name: "Lang",
                table: "TaxoboxImages");

            migrationBuilder.DropColumn(
                name: "Lang",
                table: "Taxoboxes");

            migrationBuilder.DropColumn(
                name: "Lang",
                table: "TaxoboxEntries");

            migrationBuilder.DropColumn(
                name: "Lang",
                table: "ParsingResults");

            migrationBuilder.DropColumn(
                name: "Lang",
                table: "Pages");

            migrationBuilder.DropColumn(
                name: "Lang",
                table: "PageContents");

            migrationBuilder.DropColumn(
                name: "Lang",
                table: "ImageLinks");

            migrationBuilder.RenameColumn(
                name: "NameLocal",
                table: "TaxonomyEntry",
                newName: "NameDe");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaxonomyRelations",
                table: "TaxonomyRelations",
                columns: new[] { "Ancestor", "Descendant" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaxonomyEntry",
                table: "TaxonomyEntry",
                columns: new[] { "Title", "No" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_Taxoboxes",
                table: "Taxoboxes",
                column: "Title");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaxoboxEntries",
                table: "TaxoboxEntries",
                columns: new[] { "Title", "Key" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_ParsingResults",
                table: "ParsingResults",
                column: "Title");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Pages",
                table: "Pages",
                column: "Title");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PageContents",
                table: "PageContents",
                column: "Title");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ImageLinks",
                table: "ImageLinks",
                columns: new[] { "Title", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_TaxonomyRelations_Descendant_Ancestor",
                table: "TaxonomyRelations",
                columns: new[] { "Descendant", "Ancestor" })
                .Annotation("SqlServer:Include", new[] { "No" });

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
                name: "IX_Pages_Step_Type",
                table: "Pages",
                columns: new[] { "Step", "Type" });

            migrationBuilder.AddForeignKey(
                name: "FK_ImageLinks_Pages_Title",
                table: "ImageLinks",
                column: "Title",
                principalTable: "Pages",
                principalColumn: "Title",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PageContents_Pages_Title",
                table: "PageContents",
                column: "Title",
                principalTable: "Pages",
                principalColumn: "Title",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ParsingResults_Pages_Title",
                table: "ParsingResults",
                column: "Title",
                principalTable: "Pages",
                principalColumn: "Title",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaxoboxEntries_ParsingResults_Title",
                table: "TaxoboxEntries",
                column: "Title",
                principalTable: "ParsingResults",
                principalColumn: "Title",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Taxoboxes_Pages_Title",
                table: "Taxoboxes",
                column: "Title",
                principalTable: "Pages",
                principalColumn: "Title",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaxonomyEntry_ParsingResults_Title",
                table: "TaxonomyEntry",
                column: "Title",
                principalTable: "ParsingResults",
                principalColumn: "Title",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TaxonomyRelations_Pages_Ancestor",
                table: "TaxonomyRelations",
                column: "Ancestor",
                principalTable: "Pages",
                principalColumn: "Title");

            migrationBuilder.AddForeignKey(
                name: "FK_TaxonomyRelations_Pages_Descendant",
                table: "TaxonomyRelations",
                column: "Descendant",
                principalTable: "Pages",
                principalColumn: "Title");
        }
    }
}
