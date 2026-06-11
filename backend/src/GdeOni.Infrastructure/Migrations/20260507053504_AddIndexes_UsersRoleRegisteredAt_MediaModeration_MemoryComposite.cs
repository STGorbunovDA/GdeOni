using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIndexes_UsersRoleRegisteredAt_MediaModeration_MemoryComposite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_users_registered_at_utc",
                table: "users",
                column: "registered_at_utc",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_users_role",
                table: "users",
                column: "role");

            migrationBuilder.CreateIndex(
                name: "ix_memory_entries_deceased_id_moderation_status",
                table: "deceased_memory_entries",
                columns: new[] { "deceased_id", "moderation_status" });

            migrationBuilder.CreateIndex(
                name: "ix_deceased_media_moderation_status",
                table: "deceased_media",
                column: "moderation_status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_registered_at_utc",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_role",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_memory_entries_deceased_id_moderation_status",
                table: "deceased_memory_entries");

            migrationBuilder.DropIndex(
                name: "ix_deceased_media_moderation_status",
                table: "deceased_media");
        }
    }
}
