using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Creational.Migrations
{
    public partial class TaxoboxImages : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ImageData",
                columns: table => new
                {
                    Filename = table.Column<string>(type: "varchar(2000)", unicode: false, maxLength: 2000, nullable: false, collation: "Latin1_General_BIN2"),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    ContentType = table.Column<string>(type: "varchar(120)", unicode: false, maxLength: 120, nullable: true),
                    Data = table.Column<byte[]>(type: "varbinary(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ImageData", x => x.Filename);
                });

            migrationBuilder.CreateTable(
                name: "TaxoboxImages",
                columns: table => new
                {
                    Title = table.Column<string>(type: "varchar(200)", unicode: false, maxLength: 200, nullable: false),
                    Filename = table.Column<string>(type: "varchar(2000)", unicode: false, maxLength: 2000, nullable: true, collation: "Latin1_General_BIN2")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxoboxImages", x => x.Title);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ImageData");

            migrationBuilder.DropTable(
                name: "TaxoboxImages");
        }
    }
}
