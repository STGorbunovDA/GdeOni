using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserNameNormalized : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Снимаем старый unique-индекс с user_name (display-форма).
            migrationBuilder.DropIndex(
                name: "ux_users_user_name",
                table: "users");

            // Добавляем колонку как nullable, чтобы сначала заполнить
            // её для существующих строк — иначе DEFAULT '' конфликтует
            // с unique-индексом при >1 юзере.
            migrationBuilder.AddColumn<string>(
                name: "user_name_normalized",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE users SET user_name_normalized = LOWER(user_name);");

            migrationBuilder.AlterColumn<string>(
                name: "user_name_normalized",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            // Уникальный индекс теперь на нормализованной форме.
            migrationBuilder.CreateIndex(
                name: "ux_users_user_name",
                table: "users",
                column: "user_name_normalized",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_users_user_name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "user_name_normalized",
                table: "users");

            migrationBuilder.CreateIndex(
                name: "ux_users_user_name",
                table: "users",
                column: "user_name",
                unique: true);
        }
    }
}
