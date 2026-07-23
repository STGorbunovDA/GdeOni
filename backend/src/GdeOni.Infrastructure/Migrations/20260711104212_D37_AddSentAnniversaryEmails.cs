using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class D37_AddSentAnniversaryEmails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "sent_anniversary_emails",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deceased_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    anniversary_date = table.Column<DateOnly>(type: "date", nullable: false),
                    sent_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sent_anniversary_emails", x => x.id);
                    table.ForeignKey(
                        name: "fk_sent_anniversary_emails_deceased_records_deceased_id",
                        column: x => x.deceased_id,
                        principalTable: "deceased_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_sent_anniversary_emails_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_sent_anniversary_emails_deceased_id",
                table: "sent_anniversary_emails",
                column: "deceased_id");

            migrationBuilder.CreateIndex(
                name: "ux_sent_anniversary_emails_user_deceased_kind_date",
                table: "sent_anniversary_emails",
                columns: new[] { "user_id", "deceased_id", "kind", "anniversary_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sent_anniversary_emails");
        }
    }
}
