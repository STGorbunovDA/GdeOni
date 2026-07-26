using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUserNameUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_users_user_name",
                table: "users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ux_users_user_name",
                table: "users",
                column: "user_name_normalized",
                unique: true);
        }
    }
}
