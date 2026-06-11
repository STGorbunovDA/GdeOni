using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GdeOni.Infrastructure.Migrations
{
    /// <summary>
    /// Пустая миграция. Была сгенерирована случайно (`dotnet ef migrations add`
    /// без реальных изменений модели). Удалять нельзя — она уже применена
    /// к dev-БД и записана в __EFMigrationsHistory; если убрать файл,
    /// `database update` упадёт с "missing migration". Оставлено как есть.
    /// </summary>
    public partial class ChangeTable2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Пустая миграция — см. summary выше.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Пустая миграция — см. summary выше.
        }
    }
}
