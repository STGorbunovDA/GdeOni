using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <inheritdoc />
    /// <remarks>
    /// pg_trgm + GIN-индексы для ILike-поиска. Реальный эффект:
    /// раньше WHERE first_name ILIKE '%дмитрий%' = sequential scan;
    /// на 50К записей это сотни ms. С GIN'ом по trigram'ам — единицы ms.
    ///
    /// Покрываем:
    /// - deceased_records: first_name, last_name, middle_name, country, city
    ///   (по этим полям DeceasedRepository.GetPaged ILIKE'ает в search+
    ///   firstName/lastName/middleName/country/city фильтрах);
    /// - users: email, user_name (UserRepository.GetPaged по search).
    ///
    /// CONCURRENTLY НЕ используем — на проде это нужно, но миграции
    /// исторически делались блокирующими, объём данных пока маленький
    /// (5 юзеров локально). Когда таблица вырастет, отдельная миграция
    /// "REINDEX CONCURRENTLY".
    /// </remarks>
    public partial class AddPgTrgmIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // extension должен ставить SUPERUSER; в нашем dev'е postgres
            // — superuser, в проде надо разово выполнить руками или
            // дать роли CREATE EXTENSION. IF NOT EXISTS — повторный
            // запуск миграции не упадёт.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            // deceased_records: 5 индексов по полям, по которым GetPaged
            // делает ILIKE. gin_trgm_ops поддерживает LIKE/ILIKE с
            // wildcard'ами на обоих концах (%text%).
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_deceased_records_first_name_trgm " +
                "ON deceased_records USING GIN (first_name gin_trgm_ops);");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_deceased_records_last_name_trgm " +
                "ON deceased_records USING GIN (last_name gin_trgm_ops);");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_deceased_records_middle_name_trgm " +
                "ON deceased_records USING GIN (middle_name gin_trgm_ops) " +
                "WHERE middle_name IS NOT NULL;");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_deceased_records_country_trgm " +
                "ON deceased_records USING GIN (country gin_trgm_ops) " +
                "WHERE country IS NOT NULL;");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_deceased_records_city_trgm " +
                "ON deceased_records USING GIN (city gin_trgm_ops) " +
                "WHERE city IS NOT NULL;");

            // users: email + user_name. UserRepository.GetPaged
            // ILIKE'ает по обоим в фильтре search.
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_users_email_trgm " +
                "ON users USING GIN (email gin_trgm_ops);");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_users_user_name_trgm " +
                "ON users USING GIN (user_name gin_trgm_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_users_user_name_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_users_email_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_deceased_records_city_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_deceased_records_country_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_deceased_records_middle_name_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_deceased_records_last_name_trgm;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS ix_deceased_records_first_name_trgm;");
            // Extension не дропаем — может использоваться другими табл.
        }
    }
}
