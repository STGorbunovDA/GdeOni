using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHolidayReminders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "holiday_reminders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    holiday_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    lead_days = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_holiday_reminders", x => x.id);
                    table.ForeignKey(
                        name: "fk_holiday_reminders_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ux_holiday_reminders_user_id_holiday_key",
                table: "holiday_reminders",
                columns: new[] { "user_id", "holiday_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "holiday_reminders");
        }
    }
}
