using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SENG302.Api.Migrations
{
    /// <inheritdoc />
    public partial class Notifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Albums",
                columns: table => new
                {
                    TripId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PhotoIds = table.Column<List<int>>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Albums", x => x.TripId);
                });

            migrationBuilder.CreateTable(
                name: "CustomFiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    FileKey = table.Column<string>(type: "text", nullable: false),
                    OriginalFileName = table.Column<string>(type: "text", nullable: false),
                    MimeType = table.Column<string>(type: "text", nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    Tags = table.Column<List<string>>(type: "text[]", nullable: true),
                    TagError = table.Column<string>(type: "text", nullable: true),
                    InTagQueue = table.Column<bool>(type: "boolean", nullable: false),
                    DateCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CanRetryTagging = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    PostId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OsmId = table.Column<long>(type: "bigint", nullable: false),
                    OsmType = table.Column<string>(type: "text", nullable: false),
                    Lat = table.Column<string>(type: "text", nullable: false),
                    Lon = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.PostId);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    ReadStatus = table.Column<bool>(type: "boolean", nullable: false),
                    HeaderText = table.Column<string>(type: "text", nullable: false),
                    BodyText = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<long>(type: "bigint", nullable: false),
                    NotificationType = table.Column<string>(type: "character varying(21)", maxLength: 21, nullable: false),
                    Handled = table.Column<bool>(type: "boolean", nullable: false),
                    TripId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Posts",
                columns: table => new
                {
                    PostId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TripId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OsmId = table.Column<long>(type: "bigint", nullable: false),
                    OsmType = table.Column<string>(type: "text", nullable: false),
                    LocationName = table.Column<string>(type: "text", nullable: false),
                    PhotoIds = table.Column<List<int>>(type: "integer[]", nullable: false),
                    CreationTime = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Posts", x => x.PostId);
                });

            migrationBuilder.CreateTable(
                name: "Scrapboards",
                columns: table => new
                {
                    TripId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    OwnerId = table.Column<int>(type: "integer", nullable: false),
                    FileKey = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Scrapboards", x => x.TripId);
                });

            migrationBuilder.CreateTable(
                name: "Trips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    HasScrapboard = table.Column<bool>(type: "boolean", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Collaborators = table.Column<List<int>>(type: "integer[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trips", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PasswordKey = table.Column<string>(type: "text", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false),
                    ProfilePicture = table.Column<int>(type: "integer", nullable: false),
                    ProfilePictureOffsetX = table.Column<float>(type: "real", nullable: false),
                    ProfilePictureOffsetY = table.Column<float>(type: "real", nullable: false),
                    ProfilePictureZoom = table.Column<float>(type: "real", nullable: false),
                    OneTimeCode = table.Column<string>(type: "character varying(6)", maxLength: 6, nullable: false),
                    CodeGenerationTime = table.Column<long>(type: "bigint", nullable: false),
                    TimeCreated = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                    AcceptedAiPrivacyPolicy = table.Column<bool>(type: "boolean", nullable: false),
                    AiTaggingEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    PreferenceMapVisible = table.Column<bool>(type: "boolean", nullable: false),
                    ForgotIsResettingPassword = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Users_Email",
                table: "Users",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Albums");

            migrationBuilder.DropTable(
                name: "CustomFiles");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "Posts");

            migrationBuilder.DropTable(
                name: "Scrapboards");

            migrationBuilder.DropTable(
                name: "Trips");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
