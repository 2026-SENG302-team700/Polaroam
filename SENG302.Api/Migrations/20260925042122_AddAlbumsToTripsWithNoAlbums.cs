using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SENG302.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAlbumsToTripsWithNoAlbums : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Written with ChatGPT-5.6 Luna
            migrationBuilder.Sql(
                """
                INSERT INTO "Albums" ("TripId", "PhotoIds")
                SELECT t.Id, '[]'
                FROM "Trips" t
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM "Albums" a
                    WHERE a."TripId" = t.Id
                );
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
