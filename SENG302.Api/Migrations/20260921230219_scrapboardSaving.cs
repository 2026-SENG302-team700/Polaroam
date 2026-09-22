using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SENG302.Api.Migrations
{
    /// <inheritdoc />
    public partial class scrapboardSaving : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "OwnerId",
                table: "Scrapboards",
                newName: "Thumbnail");

            migrationBuilder.RenameColumn(
                name: "FileKey",
                table: "Scrapboards",
                newName: "Elements");

            migrationBuilder.AddColumn<List<int>>(
                name: "Files",
                table: "Scrapboards",
                type: "integer[]",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Files",
                table: "Scrapboards");

            migrationBuilder.RenameColumn(
                name: "Thumbnail",
                table: "Scrapboards",
                newName: "OwnerId");

            migrationBuilder.RenameColumn(
                name: "Elements",
                table: "Scrapboards",
                newName: "FileKey");
        }
    }
}
