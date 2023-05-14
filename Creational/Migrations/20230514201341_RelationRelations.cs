using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Creational.Migrations
{
    public partial class RelationRelations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Descendant",
                table: "TaxonomyRelations",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldUnicode: false,
                oldMaxLength: 80);

            migrationBuilder.AlterColumn<string>(
                name: "Ancestor",
                table: "TaxonomyRelations",
                type: "varchar(200)",
                unicode: false,
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(80)",
                oldUnicode: false,
                oldMaxLength: 80);

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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TaxonomyRelations_Pages_Ancestor",
                table: "TaxonomyRelations");

            migrationBuilder.DropForeignKey(
                name: "FK_TaxonomyRelations_Pages_Descendant",
                table: "TaxonomyRelations");

            migrationBuilder.AlterColumn<string>(
                name: "Descendant",
                table: "TaxonomyRelations",
                type: "varchar(80)",
                unicode: false,
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldUnicode: false,
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Ancestor",
                table: "TaxonomyRelations",
                type: "varchar(80)",
                unicode: false,
                maxLength: 80,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(200)",
                oldUnicode: false,
                oldMaxLength: 200);
        }
    }
}
