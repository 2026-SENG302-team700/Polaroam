using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SENG302.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingInvitesToTrip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "PendingInvites",
                table: "Trips",
                type: "text[]",
                nullable: false);

            migrationBuilder.AlterColumn<long>(
                name: "Timestamp",
                table: "Notifications",
                type: "bigint",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PendingInvites",
                table: "Trips");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "Timestamp",
                table: "Notifications",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(long),
                oldType: "bigint");
        }
    }
}
