using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Creational.Migrations
{
    public partial class Taxonomy2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TaxonomyEntry",
                table: "TaxonomyEntry");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaxonomyEntry",
                table: "TaxonomyEntry",
                columns: new[] { "Title", "No" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_TaxonomyEntry",
                table: "TaxonomyEntry");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TaxonomyEntry",
                table: "TaxonomyEntry",
                columns: new[] { "Title", "Name" });
        }
    }
}
