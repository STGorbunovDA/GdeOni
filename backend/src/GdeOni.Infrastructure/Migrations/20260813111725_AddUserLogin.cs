using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <summary>
    /// Логин — уникальный идентификатор для входа (вход по email ИЛИ логину).
    /// Отличается от user_name, который отображаемый и намеренно НЕ уникален
    /// (см. RemoveUserNameUniqueIndex — тёзки допустимы).
    ///
    /// Порядок важен: сначала колонка, потом backfill существующим
    /// пользователям, и только затем уникальный индекс. Если создать индекс
    /// сразу после AddColumn (как сгенерировал бы EF по умолчанию), миграция
    /// упадёт — у всех строк был бы одинаковый пустой логин.
    /// </summary>
    public partial class AddUserLogin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "login",
                table: "users",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            // Backfill: логин = часть email до «@» (ots4@yandex.ru → «ots4»),
            // очищенная до [a-z0-9._-]. Коллизии («ivan@mail.ru» и
            // «ivan@yandex.ru») разводим числовым суффиксом.
            //
            // Цикл, а не оконная функция с ROW_NUMBER: суффикс «ivan2» сам мог
            // бы столкнуться с реально существующим логином «ivan2» из другого
            // email. Здесь каждый кандидат проверяется на занятость по факту.
            // Правила зеркалят User.GenerateLoginFromEmail в домене.
            migrationBuilder.Sql(@"
DO $$
DECLARE
    r          RECORD;
    base_login text;
    candidate  text;
    suffix     int;
BEGIN
    FOR r IN SELECT id, email FROM users ORDER BY registered_at_utc, id LOOP
        base_login := regexp_replace(lower(split_part(r.email, '@', 1)), '[^a-z0-9._-]', '', 'g');
        base_login := btrim(base_login, '._-');

        IF base_login = '' THEN
            base_login := 'user';
        END IF;

        IF length(base_login) < 3 THEN
            base_login := rpad(base_login, 3, '0');
        END IF;

        IF length(base_login) > 100 THEN
            base_login := left(base_login, 100);
        END IF;

        candidate := base_login;
        suffix := 1;

        WHILE EXISTS (SELECT 1 FROM users WHERE login = candidate) LOOP
            suffix := suffix + 1;
            candidate := left(base_login, 100 - length(suffix::text)) || suffix::text;
        END LOOP;

        UPDATE users SET login = candidate WHERE id = r.id;
    END LOOP;
END $$;");

            migrationBuilder.CreateIndex(
                name: "ux_users_login",
                table: "users",
                column: "login",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_users_login",
                table: "users");

            migrationBuilder.DropColumn(
                name: "login",
                table: "users");
        }
    }
}
