using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class D24_AddDeceasedEditsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "deceased_edits",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deceased_id = table.Column<Guid>(type: "uuid", nullable: false),
                    edited_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    edited_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    changes_json = table.Column<string>(type: "jsonb", maxLength: 8192, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deceased_edits", x => x.id);
                    table.ForeignKey(
                        name: "fk_deceased_edits_deceased_records_deceased_id",
                        column: x => x.deceased_id,
                        principalTable: "deceased_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_deceased_edits_users_edited_by_user_id",
                        column: x => x.edited_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_deceased_edits_deceased_id_edited_at_utc",
                table: "deceased_edits",
                columns: new[] { "deceased_id", "edited_at_utc" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_deceased_edits_edited_by_user_id",
                table: "deceased_edits",
                column: "edited_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deceased_edits");
        }
    }
}
