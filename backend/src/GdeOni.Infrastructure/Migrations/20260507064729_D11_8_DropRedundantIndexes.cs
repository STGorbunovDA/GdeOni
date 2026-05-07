using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class D11_8_DropRedundantIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_memory_entries_deceased_id",
                table: "deceased_memory_entries");

            migrationBuilder.DropIndex(
                name: "ix_deceased_media_kind",
                table: "deceased_media");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_memory_entries_deceased_id",
                table: "deceased_memory_entries",
                column: "deceased_id");

            migrationBuilder.CreateIndex(
                name: "ix_deceased_media_kind",
                table: "deceased_media",
                column: "kind");
        }
    }
}
