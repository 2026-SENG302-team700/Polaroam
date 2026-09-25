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

            // If OwnerId/FileKey's original types don't match Thumbnail (int) /
            // Elements (text), add explicit AlterColumn calls here to convert them.
            // e.g.:
            // migrationBuilder.AlterColumn<int>(
            //     name: "Thumbnail",
            //     table: "Scrapboards",
            //     type: "integer",
            //     nullable: true,
            //     oldClrType: typeof(Guid), // whatever OwnerId actually was
            //     oldType: "uuid");

            // Step 1: add Files as nullable first, no default constraint required
            migrationBuilder.AddColumn<List<int>>(
                name: "Files",
                table: "Scrapboards",
                type: "integer[]",
                nullable: true);

            // Step 2: backfill existing rows with an empty array
            migrationBuilder.Sql(
                """
                UPDATE "Scrapboards"
                SET "Files" = '{}'
                WHERE "Files" IS NULL;
                """);

            // Step 3: now safely tighten to non-nullable
            migrationBuilder.AlterColumn<List<int>>(
                name: "Files",
                table: "Scrapboards",
                type: "integer[]",
                nullable: false,
                oldClrType: typeof(List<int>),
                oldType: "integer[]",
                oldNullable: true,
                defaultValueSql: "'{}'");
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

            // If you added AlterColumn calls above to convert types after the
            // rename, revert those type changes here too, in reverse order.
        }
    }
}