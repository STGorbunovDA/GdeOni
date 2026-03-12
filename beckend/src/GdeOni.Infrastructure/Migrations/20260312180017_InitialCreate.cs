using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    user_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    full_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    password_hash = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    registered_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_login_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "deceased",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    first_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    last_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    middle_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    birth_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    death_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: false),
                    longitude = table.Column<double>(type: "double precision", nullable: false),
                    country = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    region = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    city = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    cemetery_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    plot_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    grave_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    location_accuracy = table.Column<int>(type: "integer", nullable: false),
                    short_description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    biography = table.Column<string>(type: "text", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_verified = table.Column<bool>(type: "boolean", nullable: false),
                    Metadata = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deceased", x => x.id);
                    table.ForeignKey(
                        name: "fk_deceased_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "deceased_photos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    added_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    moderation_status = table.Column<int>(type: "integer", nullable: false),
                    deceased_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_deceased_photos", x => x.id);
                    table.ForeignKey(
                        name: "fk_deceased_photos_deceased_deceased_id",
                        column: x => x.deceased_id,
                        principalTable: "deceased",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_deceased_photos_users_added_by_user_id",
                        column: x => x.added_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "memory_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    author_display_name = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    author_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    moderation_status = table.Column<int>(type: "integer", nullable: false),
                    deceased_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_memory_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_memory_entries_deceased_deceased_id",
                        column: x => x.deceased_id,
                        principalTable: "deceased",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_memory_entries_users_author_user_id",
                        column: x => x.author_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "tracked_deceased",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    deceased_id = table.Column<Guid>(type: "uuid", nullable: false),
                    relationship_type = table.Column<int>(type: "integer", nullable: false),
                    personal_notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    notify_on_death_anniversary = table.Column<bool>(type: "boolean", nullable: false),
                    notify_on_birth_anniversary = table.Column<bool>(type: "boolean", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    tracked_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tracked_deceased", x => x.id);
                    table.ForeignKey(
                        name: "fk_tracked_deceased_deceased_deceased_id",
                        column: x => x.deceased_id,
                        principalTable: "deceased",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_tracked_deceased_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_deceased_created_at_utc",
                table: "deceased",
                column: "created_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_deceased_created_by_user_id",
                table: "deceased",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_deceased_is_verified",
                table: "deceased",
                column: "is_verified");

            migrationBuilder.CreateIndex(
                name: "ix_deceased_photos_added_by_user_id",
                table: "deceased_photos",
                column: "added_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_deceased_photos_deceased_id",
                table: "deceased_photos",
                column: "deceased_id");

            migrationBuilder.CreateIndex(
                name: "ix_memory_entries_author_user_id",
                table: "memory_entries",
                column: "author_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_memory_entries_deceased_id",
                table: "memory_entries",
                column: "deceased_id");

            migrationBuilder.CreateIndex(
                name: "ix_tracked_deceased_deceased_id",
                table: "tracked_deceased",
                column: "deceased_id");

            migrationBuilder.CreateIndex(
                name: "ix_tracked_deceased_user_id",
                table: "tracked_deceased",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ux_tracked_deceased_user_id_deceased_id",
                table: "tracked_deceased",
                columns: new[] { "user_id", "deceased_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ux_users_user_name",
                table: "users",
                column: "user_name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deceased_photos");

            migrationBuilder.DropTable(
                name: "memory_entries");

            migrationBuilder.DropTable(
                name: "tracked_deceased");

            migrationBuilder.DropTable(
                name: "deceased");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
