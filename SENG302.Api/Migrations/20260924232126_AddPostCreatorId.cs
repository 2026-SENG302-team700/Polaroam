using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SENG302.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPostCreatorId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Add the column as nullable so existing rows are valid
            migrationBuilder.AddColumn<int>(
                name: "CreatorId",
                table: "Posts",
                type: "integer",
                nullable: true);

            // 2. Backfill from the trip's owner
            migrationBuilder.Sql(@"
                UPDATE ""Posts""
                SET ""CreatorId"" = (
                    SELECT ""UserId""
                    FROM ""Trips""
                    WHERE ""Trips"".""Id"" = ""Posts"".""TripId""
                )");

            // 3. Now that every row has a value, make it non-nullable
            migrationBuilder.AlterColumn<int>(
                name: "CreatorId",
                table: "Posts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatorId",
                table: "Posts");
        }
    }
}