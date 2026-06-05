using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockedByAndComplimentaryByForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cleanup orphan-ссылок ДО добавления FK: если на dev/prod
            // среде остались записи, где blocked_by или complimentary_by
            // указывают на удалённого юзера, PostgreSQL откажет в
            // добавлении constraint. SET NULL обнуляет такие зависшие
            // ссылки — данные о блокировке/complimentary остаются, просто
            // "кем" становится анонимно. На локальной БД таких записей нет
            // (проверено), но на чужой среде миграция бы упала.
            migrationBuilder.Sql(@"
                UPDATE users SET blocked_by_user_id = NULL
                WHERE blocked_by_user_id IS NOT NULL
                  AND blocked_by_user_id NOT IN (SELECT id FROM users);

                UPDATE users SET complimentary_access_granted_by_admin_id = NULL
                WHERE complimentary_access_granted_by_admin_id IS NOT NULL
                  AND complimentary_access_granted_by_admin_id NOT IN (SELECT id FROM users);
            ");

            migrationBuilder.CreateIndex(
                name: "ix_users_blocked_by_user_id",
                table: "users",
                column: "blocked_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_users_complimentary_access_granted_by_admin_id",
                table: "users",
                column: "complimentary_access_granted_by_admin_id");

            migrationBuilder.AddForeignKey(
                name: "fk_users_users_blocked_by_user_id",
                table: "users",
                column: "blocked_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_users_users_complimentary_access_granted_by_admin_id",
                table: "users",
                column: "complimentary_access_granted_by_admin_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_users_users_blocked_by_user_id",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "fk_users_users_complimentary_access_granted_by_admin_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_blocked_by_user_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_complimentary_access_granted_by_admin_id",
                table: "users");
        }
    }
}
