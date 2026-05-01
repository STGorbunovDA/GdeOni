using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDeceasedMainMediaId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "main_media_id",
                table: "deceased_records",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_deceased_main_media_id",
                table: "deceased_records",
                column: "main_media_id");

            migrationBuilder.AddForeignKey(
                name: "fk_deceased_records_deceased_media_main_media_id",
                table: "deceased_records",
                column: "main_media_id",
                principalTable: "deceased_media",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_deceased_records_deceased_media_main_media_id",
                table: "deceased_records");

            migrationBuilder.DropIndex(
                name: "ix_deceased_main_media_id",
                table: "deceased_records");

            migrationBuilder.DropColumn(
                name: "main_media_id",
                table: "deceased_records");
        }
    }
}
