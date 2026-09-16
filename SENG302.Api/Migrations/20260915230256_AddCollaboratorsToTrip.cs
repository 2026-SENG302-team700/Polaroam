using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SENG302.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCollaboratorsToTrip : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<int>>(
                name: "Collaborators",
                table: "Trips",
                type: "integer[]",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "Trips"
                SET "Collaborators" = ARRAY[]::integer[]
                WHERE "Collaborators" IS NULL;
                """);

            migrationBuilder.AlterColumn<List<int>>(
                name: "Collaborators",
                table: "Trips",
                type: "integer[]",
                nullable: false,
                oldNullable: true);
                }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Collaborators",
                table: "Trips");
        }
    }
}
