using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRelativeDiscoveries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "relative_discoveries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    deceased_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relative_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    discovered_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_new = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_relative_discoveries", x => x.id);
                    table.ForeignKey(
                        name: "fk_relative_discoveries_deceased_records_deceased_id",
                        column: x => x.deceased_id,
                        principalTable: "deceased_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_relative_discoveries_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_relative_discoveries_users_relative_user_id",
                        column: x => x.relative_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_relative_discoveries_deceased_id",
                table: "relative_discoveries",
                column: "deceased_id");

            migrationBuilder.CreateIndex(
                name: "ix_relative_discoveries_owner_user_id_is_new",
                table: "relative_discoveries",
                columns: new[] { "owner_user_id", "is_new" });

            migrationBuilder.CreateIndex(
                name: "ix_relative_discoveries_relative_user_id",
                table: "relative_discoveries",
                column: "relative_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_relative_discoveries_owner_deceased_relative",
                table: "relative_discoveries",
                columns: new[] { "owner_user_id", "deceased_id", "relative_user_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "relative_discoveries");
        }
    }
}
