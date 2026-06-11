using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// D31. pg_trgm + GIN-индексы для ILike-поиска по support_tickets.
    /// SupportTicketRepository.GetPagedForAdmin ILIKE'ает по title,
    /// description и users.email. Email уже покрыт ix_users_email_trgm
    /// (миграция AddPgTrgmIndexes). Здесь добавляем недостающие два:
    /// title и description.
    ///
    /// Эффект: пока тикетов десятки — full-table scan занимает единицы
    /// ms, разницы не видно. Когда вырастет до 10k+ — без GIN'а
    /// ILIKE '%term%' идёт sequential scan по всей таблице, с GIN'ом —
    /// единицы ms. Заранее это не критично, но индексы дешёвые
    /// (несколько MB на типичный datasize), лучше поставить сразу
    /// чем потом разбираться когда заметят пользователи.
    ///
    /// Extension pg_trgm уже создан в AddPgTrgmIndexes; повторный
    /// CREATE EXTENSION IF NOT EXISTS — no-op.
    /// </remarks>
    public partial class AddPgTrgmIndexesForSupportTickets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Подстраховка — если миграция применяется на свежей БД
            // без AddPgTrgmIndexes (теоретически невозможно, EF их
            // упорядочит), extension всё равно будет.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_support_tickets_title_trgm " +
                "ON support_tickets USING GIN (title gin_trgm_ops);");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_support_tickets_description_trgm " +
                "ON support_tickets USING GIN (description gin_trgm_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_support_tickets_description_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_support_tickets_title_trgm;");
            // Extension не дропаем — используется другими индексами
            // (ix_deceased_records_*_trgm, ix_users_*_trgm).
        }
    }
}
