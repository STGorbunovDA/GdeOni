using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IntroduceDeceasedMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "deceased_media",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deceased_id = table.Column<Guid>(type: "uuid", nullable: false),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<int>(type: "integer", nullable: false),
                    original_file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    bucket = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    content_type = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_main_photo = table.Column<bool>(type: "boolean", nullable: false),
                    moderation_status = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deceased_media", x => x.id);
                    table.ForeignKey(
                        name: "fk_deceased_media_deceased_records_deceased_id",
                        column: x => x.deceased_id,
                        principalTable: "deceased_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_deceased_media_users_uploaded_by_user_id",
                        column: x => x.uploaded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "ix_deceased_media_deceased_id",
                table: "deceased_media",
                column: "deceased_id");

            migrationBuilder.CreateIndex(
                name: "ix_deceased_media_kind",
                table: "deceased_media",
                column: "kind");

            migrationBuilder.CreateIndex(
                name: "ix_deceased_media_uploaded_by_user_id",
                table: "deceased_media",
                column: "uploaded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ux_deceased_media_storage_key",
                table: "deceased_media",
                column: "storage_key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deceased_media");
        }
    }
}
