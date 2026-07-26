using System.Globalization;

namespace GdeOni.Application.Events;

/// <summary>
/// Чистый календарь праздников: по диапазону дат возвращает памятные
/// даты четырёх категорий. Подвижные даты считаются формулами
/// (православная Пасха — алгоритм Гаусса/Мееуса по юлианскому календарю
/// с переводом в григорианский; поминальные дни — смещения от Пасхи;
/// мусульманские — через <see cref="UmAlQuraCalendar"/>). Никакой БД —
/// поэтому логика покрыта юнит-тестами.
///
/// Мусульманские даты табличные (UmAlQura) и могут отличаться от
/// фактически объявленных по новолунию на ±1 день — это нормально для
/// справочного блока.
/// </summary>
public static class HolidayCalculator
{
    /// <summary>
    /// Все праздники, попадающие в диапазон [<paramref name="from"/>,
    /// <paramref name="to"/>] включительно, отсортированные по дате.
    /// </summary>
    public static IReadOnlyList<Holiday> GetHolidays(DateOnly from, DateOnly to)
    {
        if (to < from)
            return Array.Empty<Holiday>();

        var result = new List<Holiday>();
        for (var year = from.Year; year <= to.Year; year++)
            AddYear(result, year);

        return result
            .Where(h => h.Date >= from && h.Date <= to)
            .OrderBy(h => h.Date)
            .ThenBy(h => h.Category)
            .ToList();
    }

    /// <summary>
    /// Дата православной Пасхи в григорианском календаре для указанного
    /// года. Алгоритм даёт юлианскую дату, смещение Julian→Gregorian
    /// считается общей формулой (13 дней для 1900–2099).
    /// </summary>
    public static DateOnly OrthodoxEaster(int year)
    {
        var a = year % 4;
        var b = year % 7;
        var c = year % 19;
        var d = (19 * c + 15) % 30;
        var e = (2 * a + 4 * b - d + 34) % 7;
        var month = (d + e + 114) / 31;        // 3 (март) или 4 (апрель), юлианский
        var day = ((d + e + 114) % 31) + 1;

        var julian = new DateOnly(year, month, day);
        var offset = year / 100 - year / 400 - 2;   // Julian → Gregorian
        return julian.AddDays(offset);
    }

    private static void AddYear(List<Holiday> list, int year)
    {
        AddFixedOrthodox(list, year);
        AddMovableOrthodox(list, year);
        AddFasts(list, year);
        AddMemorial(list, year);
        AddState(list, year);
        AddMuslim(list, year);
    }

    private static void AddFixedOrthodox(List<Holiday> list, int year)
    {
        void Add(int month, int day, string name, bool isMajor = false) =>
            list.Add(new Holiday(new DateOnly(year, month, day), name, HolidayCategory.Orthodox, isMajor));

        // Двунадесятые непереходящие (девять; ещё три — переходящие,
        // см. AddMovableOrthodox). Все — крупные (isMajor: true).
        Add(1, 7, "Рождество Христово", isMajor: true);
        Add(1, 19, "Крещение Господне (Богоявление)", isMajor: true);
        Add(2, 15, "Сретение Господне", isMajor: true);
        Add(4, 7, "Благовещение Пресвятой Богородицы", isMajor: true);
        Add(8, 19, "Преображение Господне (Яблочный Спас)", isMajor: true);
        Add(8, 28, "Успение Пресвятой Богородицы", isMajor: true);
        Add(9, 21, "Рождество Пресвятой Богородицы", isMajor: true);
        Add(9, 27, "Воздвижение Креста Господня", isMajor: true);
        Add(12, 4, "Введение во храм Пресвятой Богородицы", isMajor: true);

        // Великие НЕдвунадесятые. Изначально их не было — из-за чего
        // 12 июля (Петров день) не показывался, хотя это один из самых
        // заметных дней в народном календаре.
        Add(1, 14, "Обрезание Господне. Память Василия Великого");
        Add(7, 7, "Рождество Иоанна Предтечи");
        Add(7, 12, "День святых апостолов Петра и Павла");
        Add(9, 11, "Усекновение главы Иоанна Предтечи");
        Add(10, 14, "Покров Пресвятой Богородицы");

        // Дни вокруг Рождества и Крещения.
        Add(1, 8, "Собор Пресвятой Богородицы");

        // Прочие широко отмечаемые даты церковного года.
        Add(5, 6, "Великомученик Георгий Победоносец (Юрьев день)");
        Add(5, 22, "Никола Вешний (перенесение мощей святителя Николая)");
        Add(5, 24, "Равноапостольные Кирилл и Мефодий");
        Add(7, 21, "Явление Казанской иконы Божией Матери");
        Add(7, 28, "Крещение Руси. Память равноапостольного князя Владимира");
        Add(8, 2, "Пророк Илия (Ильин день)");
        Add(8, 14, "Медовый Спас. Изнесение Древ Креста Господня");
        Add(8, 29, "Ореховый (Хлебный) Спас. Перенесение Нерукотворного Образа");
        Add(11, 4, "Казанская икона Божией Матери");
        Add(11, 21, "Собор Архистратига Михаила (Михайлов день)");
        Add(12, 13, "Апостол Андрей Первозванный");
        Add(12, 19, "Никола Зимний (память святителя Николая Чудотворца)");
    }

    /// <summary>
    /// Переходящие даты — смещения от Пасхи. Смещения выверены по
    /// календарю 2026 года (Пасха 12 апреля): Масленица начинается в
    /// понедельник E−55 (E−56 — это ещё воскресенье мясопустной недели),
    /// Великий пост — в понедельник E−48 (23 февраля 2026).
    /// </summary>
    private static void AddMovableOrthodox(List<Holiday> list, int year)
    {
        var easter = OrthodoxEaster(year);

        void Add(int offsetDays, string name, bool isMajor = false) =>
            list.Add(new Holiday(easter.AddDays(offsetDays), name, HolidayCategory.Orthodox, isMajor));

        Add(-55, "Начало Масленицы (Сырная седмица)");
        Add(-49, "Прощёное воскресенье");
        Add(-42, "Торжество Православия");
        // Переходящие двунадесятые + Пасха — крупные.
        Add(-7, "Вход Господень в Иерусалим (Вербное воскресенье)", isMajor: true);
        Add(-3, "Великий четверг");
        Add(-2, "Великая пятница");
        Add(-1, "Великая суббота");
        Add(0, "Пасха (Светлое Христово Воскресение)", isMajor: true);
        Add(39, "Вознесение Господне", isMajor: true);
        Add(49, "Троица (Пятидесятница)", isMajor: true);
        Add(50, "День Святого Духа (Духов день)");
        Add(56, "Неделя всех святых");
    }

    /// <summary>
    /// Посты. Отмечаем дни начала: пост длится неделями, а карточка
    /// события привязана к одной дате — «сегодня начался Петров пост»
    /// полезно, «сегодня 19-й день поста» нет.
    ///
    /// Петров пост — единственный переходящий: начинается в понедельник
    /// после Недели всех святых (E+57) и всегда кончается 11 июля,
    /// накануне Петра и Павла. Поэтому его длительность гуляет от года
    /// к году.
    /// </summary>
    private static void AddFasts(List<Holiday> list, int year)
    {
        var easter = OrthodoxEaster(year);

        // Посты — все крупные.
        void Add(DateOnly date, string name) =>
            list.Add(new Holiday(date, name, HolidayCategory.Fast, IsMajor: true));

        Add(new DateOnly(year, 1, 6), "Рождественский сочельник (строгий пост)");
        Add(new DateOnly(year, 1, 18), "Крещенский сочельник (строгий пост)");
        Add(easter.AddDays(-48), "Начало Великого поста");
        Add(easter.AddDays(57), "Начало Петрова поста");
        Add(new DateOnly(year, 8, 14), "Начало Успенского поста");
        Add(new DateOnly(year, 11, 28), "Начало Рождественского поста");
    }

    private static void AddMemorial(List<Holiday> list, int year)
    {
        var easter = OrthodoxEaster(year);

        // Поминальные/родительские дни — все крупные.
        void Add(DateOnly date, string name) =>
            list.Add(new Holiday(date, name, HolidayCategory.Memorial, IsMajor: true));

        // Подвижные, считаются смещением от Пасхи (все — субботы, кроме
        // Радоницы-вторника).
        Add(easter.AddDays(-57), "Вселенская родительская (мясопустная) суббота");
        Add(easter.AddDays(-36), "Родительская суббота 2-й седмицы Великого поста");
        Add(easter.AddDays(-29), "Родительская суббота 3-й седмицы Великого поста");
        Add(easter.AddDays(-22), "Родительская суббота 4-й седмицы Великого поста");
        Add(easter.AddDays(9), "Радоница");
        Add(easter.AddDays(48), "Троицкая родительская суббота");

        // Димитриевская родительская суббота — суббота на/перед 8 ноября.
        Add(SaturdayOnOrBefore(new DateOnly(year, 11, 8)), "Димитриевская родительская суббота");

        // Светские дни поминовения. Для сервиса про места захоронений они
        // по смыслу ближе к родительским субботам, чем к государственным
        // праздникам, — поэтому категория Memorial, а не State.
        //
        // 9 мая попадает в список дважды и это не ошибка: как
        // государственный праздник (День Победы) и как церковное
        // поминовение усопших воинов — разные категории, разные блоки.
        Add(new DateOnly(year, 5, 9), "Поминовение усопших воинов");
        Add(new DateOnly(year, 6, 22), "День памяти и скорби");
        Add(new DateOnly(year, 10, 30), "День памяти жертв политических репрессий");
        Add(new DateOnly(year, 12, 3), "День неизвестного солдата");
    }

    private static void AddState(List<Holiday> list, int year)
    {
        // Государственные праздники РФ — все крупные.
        void Add(int month, int day, string name) =>
            list.Add(new Holiday(new DateOnly(year, month, day), name, HolidayCategory.State, IsMajor: true));

        Add(1, 1, "Новый год");
        Add(2, 23, "День защитника Отечества");
        Add(3, 8, "Международный женский день");
        Add(5, 1, "Праздник Весны и Труда");
        Add(5, 9, "День Победы");
        Add(6, 12, "День России");
        Add(11, 4, "День народного единства");
    }

    private static void AddMuslim(List<Holiday> list, int year)
    {
        // Табличный исламский календарь. Хиджра для григорианского года Y
        // ≈ Y − 579; берём окно ±, чтобы поймать даты, попадающие в Y.
        var calendar = new UmAlQuraCalendar();

        for (var hijriYear = year - 580; hijriYear <= year - 577; hijriYear++)
        {
            TryAddMuslim(list, calendar, hijriYear, 1, 1, "Новый год по Хиджре", year);
            TryAddMuslim(list, calendar, hijriYear, 1, 10, "Ашура", year);
            TryAddMuslim(list, calendar, hijriYear, 3, 12, "Мавлид ан-Наби", year);
            TryAddMuslim(list, calendar, hijriYear, 9, 1, "Начало Рамадана", year);
            TryAddMuslim(list, calendar, hijriYear, 9, 27, "Ляйлятуль-Кадр (Ночь предопределения)", year);
            // Крупные — два больших праздника разговения и жертвоприношения.
            TryAddMuslim(list, calendar, hijriYear, 10, 1, "Ураза-байрам (Ид аль-Фитр)", year, isMajor: true);
            TryAddMuslim(list, calendar, hijriYear, 12, 10, "Курбан-байрам (Ид аль-Адха)", year, isMajor: true);
        }
    }

    private static void TryAddMuslim(
        List<Holiday> list,
        UmAlQuraCalendar calendar,
        int hijriYear,
        int hijriMonth,
        int hijriDay,
        string name,
        int targetGregorianYear,
        bool isMajor = false)
    {
        DateTime dateTime;
        try
        {
            dateTime = calendar.ToDateTime(hijriYear, hijriMonth, hijriDay, 0, 0, 0, 0);
        }
        catch (ArgumentOutOfRangeException)
        {
            // Год вне поддерживаемого UmAlQura диапазона — пропускаем.
            return;
        }

        if (dateTime.Year != targetGregorianYear)
            return;

        list.Add(new Holiday(DateOnly.FromDateTime(dateTime), name, HolidayCategory.Muslim, isMajor));
    }

    /// <summary>Ближайшая суббота на указанную дату или раньше.</summary>
    private static DateOnly SaturdayOnOrBefore(DateOnly date)
    {
        var offset = ((int)date.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7;
        return date.AddDays(-offset);
    }
}
