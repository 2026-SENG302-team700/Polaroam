using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SENG302.Api.Migrations
{
    /// <inheritdoc />
    public partial class FileCanRetryTag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "OsmType",
                table: "Posts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OsmType",
                table: "Locations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "CanRetryTagging",
                table: "CustomFiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DateCreated",
                table: "CustomFiles",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<bool>(
                name: "InTagQueue",
                table: "CustomFiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "TagError",
                table: "CustomFiles",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OsmType",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "OsmType",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "CanRetryTagging",
                table: "CustomFiles");

            migrationBuilder.DropColumn(
                name: "DateCreated",
                table: "CustomFiles");

            migrationBuilder.DropColumn(
                name: "InTagQueue",
                table: "CustomFiles");

            migrationBuilder.DropColumn(
                name: "TagError",
                table: "CustomFiles");
        }
    }
}
