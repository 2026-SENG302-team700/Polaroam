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
            migrationBuilder.AddColumn<int>(
                name: "CreatorId",
                table: "Posts",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("UPDATE posts JOIN trips ON posts.tripid = trips.id SET posts.creatorid = trips.userid");
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
