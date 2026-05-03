using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUserSecurityStamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Колонка как nullable, чтобы существующие строки прошли проверку.
            migrationBuilder.AddColumn<Guid>(
                name: "security_stamp",
                table: "users",
                type: "uuid",
                nullable: true);

            // 2. Каждой существующей строке выдаём уникальный UUID.
            //    gen_random_uuid() — встроенная функция Postgres 13+,
            //    в Postgres 11+ применяется per-row для VOLATILE функций.
            migrationBuilder.Sql("UPDATE users SET security_stamp = gen_random_uuid()");

            // 3. Делаем NOT NULL — теперь нет ни одной NULL-строки.
            migrationBuilder.AlterColumn<Guid>(
                name: "security_stamp",
                table: "users",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "security_stamp",
                table: "users");
        }
    }
}
