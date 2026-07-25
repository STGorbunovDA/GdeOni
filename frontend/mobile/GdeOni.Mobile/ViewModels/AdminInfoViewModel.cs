using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// F38 mobile. «Информация» — справка по системе для админа: сколько людей,
/// карточек, контента, обращений, денег. Зеркало веб-страницы AdminInfoPage.
/// Только чтение: секции плиток «число + подпись», без действий и фильтров.
/// </summary>
public partial class AdminInfoViewModel(IAdminApi adminApi) : ObservableObject
{
    [ObservableProperty] private bool _isLoading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>Строка «Данные на …» под сводкой — цифры живут своей жизнью.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasGeneratedAt))]
    private string? _generatedAt;

    public bool HasGeneratedAt => !string.IsNullOrEmpty(GeneratedAt);

    /// <summary>Секции сводки в порядке веб-страницы.</summary>
    public ObservableCollection<AdminStatSection> Sections { get; } = new();

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading) return;
        try
        {
            IsLoading = true;
            ErrorMessage = null;
            var envelope = await adminApi.GetStatsAsync();
            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось загрузить сводку.";
                return;
            }

            BuildSections(envelope.Result);
            GeneratedAt =
                $"Данные на {envelope.Result.GeneratedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture)}";
        }
        catch (ApiException apiEx)
        {
            ErrorMessage = $"HTTP {(int)apiEx.StatusCode}: {apiEx.ReasonPhrase}";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Раскладывает плоские счётчики бэка по секциям — те же группы и подписи,
    /// что на вебе (AdminInfoPage.tsx), чтобы две справки не разъезжались.
    /// </summary>
    private void BuildSections(AdminStatsResponse stats)
    {
        Sections.Clear();
        var u = stats.Users;
        var d = stats.Deceased;
        var c = stats.Content;
        var s = stats.Support;
        var p = stats.Payments;

        Sections.Add(new AdminStatSection("Пользователи", new[]
        {
            new AdminStatMetric(u.Total.ToString(), "Всего зарегистрировано"),
            new AdminStatMetric(u.NewLast7Days.ToString(), "Новых за 7 дней"),
            new AdminStatMetric(u.NewLast30Days.ToString(), "Новых за 30 дней"),
            new AdminStatMetric(u.ActiveLast30Days.ToString(), "Заходили за 30 дней"),
            new AdminStatMetric(u.Admins.ToString(), "Администраторов"),
            new AdminStatMetric(u.Blocked.ToString(), "Заблокировано"),
        }));

        Sections.Add(new AdminStatSection("Доступ и подписки", new[]
        {
            new AdminStatMetric(u.WithActiveSubscription.ToString(), "С активной подпиской"),
            new AdminStatMetric(u.OnTrial.ToString(), "На пробном периоде"),
            new AdminStatMetric(u.WithComplimentaryAccess.ToString(), "Бесплатный доступ от админа"),
        }));

        Sections.Add(new AdminStatSection("Карточки умерших", new[]
        {
            new AdminStatMetric(d.Total.ToString(), "Всего карточек"),
            new AdminStatMetric(d.NewLast30Days.ToString(), "Создано за 30 дней"),
            new AdminStatMetric(d.Verified.ToString(), "Подтверждённых"),
            new AdminStatMetric(d.WithCoordinates.ToString(), "С координатами",
                "Без них не работают маршрут и «найти рядом»"),
            new AdminStatMetric(d.WithMainPhoto.ToString(), "С главным фото",
                "Остальные показываются в поиске без превью"),
            new AdminStatMetric(d.TrackedRecords.ToString(), "Подписок на отслеживание",
                "Записей «пользователь ↔ карточка», а не людей"),
        }));

        Sections.Add(new AdminStatSection("Контент", new[]
        {
            new AdminStatMetric(c.Photos.ToString(), "Фото умерших"),
            new AdminStatMetric(c.GravePhotos.ToString(), "Фото могил"),
            new AdminStatMetric(c.Documents.ToString(), "Документов"),
            new AdminStatMetric(c.Memories.ToString(), "Воспоминаний"),
            new AdminStatMetric(c.Edits.ToString(), "Правок карточек"),
        }));

        Sections.Add(new AdminStatSection("Поддержка", new[]
        {
            new AdminStatMetric(s.Total.ToString(), "Всего обращений"),
            new AdminStatMetric(s.Open.ToString(), "Ждут ответа", "Открытые и в работе"),
            new AdminStatMetric(s.Resolved.ToString(), "Решено"),
        }));

        Sections.Add(new AdminStatSection("Платежи", new[]
        {
            new AdminStatMetric(p.SucceededCount.ToString(), "Успешных платежей"),
            new AdminStatMetric(FormatRub(p.TotalRub), "Всего получено"),
            new AdminStatMetric(FormatRub(p.Last30DaysRub), "За 30 дней"),
        }));
    }

    /// <summary>Рубли без копеек — суммы подписки целые, дробная часть шумит.</summary>
    private static string FormatRub(decimal value)
        => $"{Math.Round(value, MidpointRounding.AwayFromZero).ToString("0", CultureInfo.InvariantCulture)} ₽";

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

/// <summary>Секция сводки: заголовок + плитки-метрики.</summary>
public sealed record AdminStatSection(string Title, IReadOnlyList<AdminStatMetric> Metrics);

/// <summary>
/// Плитка «число + подпись» (+ опциональная сноска-пояснение под подписью).
/// </summary>
public sealed record AdminStatMetric(string Value, string Label, string? Hint = null)
{
    public bool HasHint => !string.IsNullOrEmpty(Hint);
}
