using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportTicketUserActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "accepted_by_user",
                table: "support_tickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "accepted_by_user_at_utc",
                table: "support_tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "last_user_reply",
                table: "support_tickets",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_user_reply_at_utc",
                table: "support_tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reopened_count",
                table: "support_tickets",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "accepted_by_user",
                table: "support_tickets");

            migrationBuilder.DropColumn(
                name: "accepted_by_user_at_utc",
                table: "support_tickets");

            migrationBuilder.DropColumn(
                name: "last_user_reply",
                table: "support_tickets");

            migrationBuilder.DropColumn(
                name: "last_user_reply_at_utc",
                table: "support_tickets");

            migrationBuilder.DropColumn(
                name: "reopened_count",
                table: "support_tickets");
        }
    }
}
