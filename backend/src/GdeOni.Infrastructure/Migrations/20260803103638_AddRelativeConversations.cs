using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRelativeConversations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "relative_conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deceased_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_a_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_b_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_message_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_relative_conversations", x => x.id);
                    table.ForeignKey(
                        name: "fk_relative_conversations_deceased_records_deceased_id",
                        column: x => x.deceased_id,
                        principalTable: "deceased_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_relative_conversations_users_participant_a_id",
                        column: x => x.participant_a_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_relative_conversations_users_participant_b_id",
                        column: x => x.participant_b_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "relative_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sender_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    edited_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    deleted_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_read = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    read_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_relative_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_relative_messages_relative_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "relative_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_relative_conversations_last_message_at_utc",
                table: "relative_conversations",
                column: "last_message_at_utc",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "ix_relative_conversations_participant_a_id",
                table: "relative_conversations",
                column: "participant_a_id");

            migrationBuilder.CreateIndex(
                name: "ix_relative_conversations_participant_b_id",
                table: "relative_conversations",
                column: "participant_b_id");

            migrationBuilder.CreateIndex(
                name: "ux_relative_conversations_deceased_a_b",
                table: "relative_conversations",
                columns: new[] { "deceased_id", "participant_a_id", "participant_b_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_relative_messages_conversation_created",
                table: "relative_messages",
                columns: new[] { "conversation_id", "created_at_utc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "relative_messages");

            migrationBuilder.DropTable(
                name: "relative_conversations");
        }
    }
}
