using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConfirmSuperAdminEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // D45. Существующий сид-SuperAdmin, созданный до AddEmailConfirmation,
            // получил бэкфиллом is_email_confirmed=false и потому видел баннер
            // «Подтвердите email». Его почте доверяем — помечаем подтверждённым
            // и снимаем гейт/токен. Новые сид-админы уже создаются
            // подтверждёнными в домене (RegisterSuperAdmin → MarkEmailPreconfirmed).
            // role хранится строкой (HasConversion<string>()).
            migrationBuilder.Sql(@"
UPDATE users
SET is_email_confirmed = true,
    email_confirmed_at_utc = COALESCE(email_confirmed_at_utc, now()),
    email_confirmation_required = false,
    email_confirmation_token_hash = NULL,
    email_confirmation_token_expires_at_utc = NULL
WHERE role = 'SuperAdmin'
  AND is_email_confirmed = false;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Бэкфилл данных необратим по смыслу — откат не нужен (нельзя
            // достоверно вернуть «был не подтверждён»).
        }
    }
}
