using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeNameTableDeceasedFromDeceasedRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_deceased_memory_entries_deceaseds_deceased_id",
                table: "deceased_memory_entries");

            migrationBuilder.DropForeignKey(
                name: "fk_deceased_photos_deceaseds_deceased_id",
                table: "deceased_photos");

            migrationBuilder.DropForeignKey(
                name: "fk_deceaseds_users_created_by_user_id",
                table: "deceaseds");

            migrationBuilder.DropForeignKey(
                name: "fk_tracked_deceased_deceaseds_deceased_id",
                table: "tracked_deceased");

            migrationBuilder.DropPrimaryKey(
                name: "pk_deceaseds",
                table: "deceaseds");

            migrationBuilder.RenameTable(
                name: "deceaseds",
                newName: "deceased_records");

            migrationBuilder.AddPrimaryKey(
                name: "pk_deceased_records",
                table: "deceased_records",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_deceased_memory_entries_deceased_records_deceased_id",
                table: "deceased_memory_entries",
                column: "deceased_id",
                principalTable: "deceased_records",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_deceased_photos_deceased_records_deceased_id",
                table: "deceased_photos",
                column: "deceased_id",
                principalTable: "deceased_records",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_deceased_records_users_created_by_user_id",
                table: "deceased_records",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tracked_deceased_deceased_records_deceased_id",
                table: "tracked_deceased",
                column: "deceased_id",
                principalTable: "deceased_records",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_deceased_memory_entries_deceased_records_deceased_id",
                table: "deceased_memory_entries");

            migrationBuilder.DropForeignKey(
                name: "fk_deceased_photos_deceased_records_deceased_id",
                table: "deceased_photos");

            migrationBuilder.DropForeignKey(
                name: "fk_deceased_records_users_created_by_user_id",
                table: "deceased_records");

            migrationBuilder.DropForeignKey(
                name: "fk_tracked_deceased_deceased_records_deceased_id",
                table: "tracked_deceased");

            migrationBuilder.DropPrimaryKey(
                name: "pk_deceased_records",
                table: "deceased_records");

            migrationBuilder.RenameTable(
                name: "deceased_records",
                newName: "deceaseds");

            migrationBuilder.AddPrimaryKey(
                name: "pk_deceaseds",
                table: "deceaseds",
                column: "id");

            migrationBuilder.AddForeignKey(
                name: "fk_deceased_memory_entries_deceaseds_deceased_id",
                table: "deceased_memory_entries",
                column: "deceased_id",
                principalTable: "deceaseds",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_deceased_photos_deceaseds_deceased_id",
                table: "deceased_photos",
                column: "deceased_id",
                principalTable: "deceaseds",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_deceaseds_users_created_by_user_id",
                table: "deceaseds",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_tracked_deceased_deceaseds_deceased_id",
                table: "tracked_deceased",
                column: "deceased_id",
                principalTable: "deceaseds",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
