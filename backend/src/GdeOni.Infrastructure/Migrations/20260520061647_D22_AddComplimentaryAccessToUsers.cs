using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class D22_AddComplimentaryAccessToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "complimentary_access_granted_at_utc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "complimentary_access_granted_by_admin_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "complimentary_access_note",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "complimentary_access_until_utc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "complimentary_access_granted_at_utc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "complimentary_access_granted_by_admin_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "complimentary_access_note",
                table: "users");

            migrationBuilder.DropColumn(
                name: "complimentary_access_until_utc",
                table: "users");
        }
    }
}
