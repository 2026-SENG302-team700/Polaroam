using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SENG302.Api.Migrations
{
    /// <inheritdoc />
    public partial class CustomFileTaggingRetryVariable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InTagQueue",
                table: "CustomFiles",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InTagQueue",
                table: "CustomFiles");
        }
    }
}
