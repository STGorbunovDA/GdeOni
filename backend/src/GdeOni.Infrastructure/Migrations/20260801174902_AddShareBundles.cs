using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShareBundles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "share_bundles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deceased_ids = table.Column<Guid[]>(type: "uuid[]", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_share_bundles", x => x.id);
                    table.ForeignKey(
                        name: "fk_share_bundles_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_share_bundles_created_by_user_id",
                table: "share_bundles",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_share_bundles_expires_at_utc",
                table: "share_bundles",
                column: "expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "ux_share_bundles_code",
                table: "share_bundles",
                column: "code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "share_bundles");
        }
    }
}
