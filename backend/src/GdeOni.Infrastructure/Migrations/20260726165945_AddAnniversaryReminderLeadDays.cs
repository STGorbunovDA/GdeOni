using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAnniversaryReminderLeadDays : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // F42. Новые колонки-наборы «за сколько дней» напоминать о
            // годовщинах смерти/рождения (CSV, например «0,7»). Пусто = выкл.
            migrationBuilder.AddColumn<string>(
                name: "birth_anniversary_lead_days",
                table: "tracked_deceased",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "death_anniversary_lead_days",
                table: "tracked_deceased",
                type: "character varying(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            // Переносим данные из булевых флагов: включено → «в день» (0).
            migrationBuilder.Sql(
                "UPDATE tracked_deceased SET death_anniversary_lead_days = '0' " +
                "WHERE notify_on_death_anniversary = true;");
            migrationBuilder.Sql(
                "UPDATE tracked_deceased SET birth_anniversary_lead_days = '0' " +
                "WHERE notify_on_birth_anniversary = true;");

            migrationBuilder.DropColumn(
                name: "notify_on_birth_anniversary",
                table: "tracked_deceased");

            migrationBuilder.DropColumn(
                name: "notify_on_death_anniversary",
                table: "tracked_deceased");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "notify_on_birth_anniversary",
                table: "tracked_deceased",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "notify_on_death_anniversary",
                table: "tracked_deceased",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Обратный перенос: любой непустой набор дней → флаг включён.
            migrationBuilder.Sql(
                "UPDATE tracked_deceased SET notify_on_death_anniversary = " +
                "(death_anniversary_lead_days <> '');");
            migrationBuilder.Sql(
                "UPDATE tracked_deceased SET notify_on_birth_anniversary = " +
                "(birth_anniversary_lead_days <> '');");

            migrationBuilder.DropColumn(
                name: "birth_anniversary_lead_days",
                table: "tracked_deceased");

            migrationBuilder.DropColumn(
                name: "death_anniversary_lead_days",
                table: "tracked_deceased");
        }
    }
}
