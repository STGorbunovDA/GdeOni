using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSupportTicketMessages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "support_ticket_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                    author_kind = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    text = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_support_ticket_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_support_ticket_messages_support_tickets_ticket_id",
                        column: x => x.ticket_id,
                        principalTable: "support_tickets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_support_ticket_messages_users_author_user_id",
                        column: x => x.author_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_support_ticket_messages_author_user_id",
                table: "support_ticket_messages",
                column: "author_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_support_ticket_messages_ticket_id_created_at",
                table: "support_ticket_messages",
                columns: new[] { "ticket_id", "created_at_utc" });

            // D25.2. Перенос истории из старых полей в новую таблицу.
            // 1) Каждая существующая резолюция → Admin-сообщение.
            // 2) Каждая последняя реплика юзера (LastUserReply) → User-сообщение.
            // Порядок в чате: ResolvedAtUtc и LastUserReplyAtUtc сохраняем
            // как created_at, чтобы хронология не сломалась.
            migrationBuilder.Sql(@"
                INSERT INTO support_ticket_messages
                    (id, ticket_id, author_kind, author_user_id, text, created_at_utc)
                SELECT
                    gen_random_uuid(),
                    id,
                    'Admin',
                    resolved_by_user_id,
                    resolution_note,
                    resolved_at_utc
                FROM support_tickets
                WHERE resolution_note IS NOT NULL
                  AND resolved_at_utc IS NOT NULL;

                INSERT INTO support_ticket_messages
                    (id, ticket_id, author_kind, author_user_id, text, created_at_utc)
                SELECT
                    gen_random_uuid(),
                    id,
                    'User',
                    user_id,
                    last_user_reply,
                    last_user_reply_at_utc
                FROM support_tickets
                WHERE last_user_reply IS NOT NULL
                  AND last_user_reply_at_utc IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "support_ticket_messages");
        }
    }
}
