using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SENG302.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdatePhotoIdsAndAlbumPhotoIdsDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<int>>(
                name: "AlbumPhotoIds",
                table: "Trips",
                type: "integer[]",
                nullable: false);

            migrationBuilder.AlterColumn<List<int>>(
                name: "PhotoIds",
                table: "Posts",
                type: "integer[]",
                nullable: false,
                oldClrType: typeof(List<int>),
                oldType: "integer[]",
                oldNullable: true);

            migrationBuilder.AddColumn<List<string>>(
                name: "Tags",
                table: "CustomFiles",
                type: "text[]",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AlbumPhotoIds",
                table: "Trips");

            migrationBuilder.DropColumn(
                name: "Tags",
                table: "CustomFiles");

            migrationBuilder.AlterColumn<List<int>>(
                name: "PhotoIds",
                table: "Posts",
                type: "integer[]",
                nullable: true,
                oldClrType: typeof(List<int>),
                oldType: "integer[]");
        }
    }
}
