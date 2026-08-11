using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SENG302.Api.Migrations
{
    /// <inheritdoc />
    public partial class FixPostCreationTimeColumnName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
          DO $$
          BEGIN
              IF EXISTS (
                  SELECT 1
                  FROM information_schema.columns
                  WHERE table_name = 'Posts'
                    AND column_name = 'CreationTime'
              )
              AND NOT EXISTS (
                  SELECT 1
                  FROM information_schema.columns
                  WHERE table_name = 'Posts'
                    AND column_name = 'creationTime'
              )
              THEN
                  ALTER TABLE "Posts"
                  RENAME COLUMN "CreationTime" TO "creationTime";
              END IF;
          END
          $$;
          """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
