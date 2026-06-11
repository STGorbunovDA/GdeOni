using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class D16_AddSubscriptionToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "subscription_cancelled_at_utc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "subscription_current_period_started_at_utc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "subscription_expires_at_utc",
                table: "users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subscription_last_payment_id",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subscription_plan",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            // Существующим пользователям выставляем Status=None: новые
            // юзеры через RegisterUserUseCase сразу попадут в Trial,
            // старые при следующем заходе на /me/subscription увидят
            // None и могут оформить подписку (либо им вручную выдать
            // Trial через admin-эндпоинт, если будет — отложено).
            // Дефолт "None" вместо пустой строки — иначе EF падает
            // при чтении (нет такого значения SubscriptionStatus).
            migrationBuilder.AddColumn<string>(
                name: "subscription_status",
                table: "users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "None");

            migrationBuilder.CreateIndex(
                name: "ix_users_subscription_expires_at_utc",
                table: "users",
                column: "subscription_expires_at_utc");

            migrationBuilder.CreateIndex(
                name: "ix_users_subscription_last_payment_id",
                table: "users",
                column: "subscription_last_payment_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_users_subscription_expires_at_utc",
                table: "users");

            migrationBuilder.DropIndex(
                name: "ix_users_subscription_last_payment_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "subscription_cancelled_at_utc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "subscription_current_period_started_at_utc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "subscription_expires_at_utc",
                table: "users");

            migrationBuilder.DropColumn(
                name: "subscription_last_payment_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "subscription_plan",
                table: "users");

            migrationBuilder.DropColumn(
                name: "subscription_status",
                table: "users");
        }
    }
}
