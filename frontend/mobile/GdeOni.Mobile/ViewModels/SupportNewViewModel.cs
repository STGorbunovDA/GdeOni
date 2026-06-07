using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Api;
using GdeOni.Mobile.Services.Api.Models;
using Refit;

namespace GdeOni.Mobile.ViewModels;

/// <summary>
/// D25 mobile. Форма создания обращения. Юзер выбирает тематику,
/// пишет заголовок и описание. Severity всегда Normal — апгрейдить
/// до Urgent может только админ из своего интерфейса.
/// </summary>
public partial class SupportNewViewModel(ISupportApi supportApi) : ObservableObject
{
    /// <summary>
    /// Локализованные подписи для UI + кодовое значение для бэка.
    /// Совпадает со строковыми значениями enum SupportTicketKind.
    /// </summary>
    public IReadOnlyList<KindOption> KindOptions { get; } = new[]
    {
        new KindOption("Платёж", "Payment"),
        new KindOption("Ошибка / Баг", "Bug"),
        new KindOption("Жалоба", "Complaint"),
        new KindOption("Вопрос", "Question"),
        new KindOption("Другое", "Other"),
    };

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    private KindOption? _selectedKind;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    private string _title = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    private string _description = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool CanSubmit =>
        !IsBusy
        && SelectedKind is not null
        && !string.IsNullOrWhiteSpace(Title)
        && !string.IsNullOrWhiteSpace(Description);

    [RelayCommand(CanExecute = nameof(CanSubmit))]
    private async Task SubmitAsync()
    {
        if (SelectedKind is null) return;
        try
        {
            IsBusy = true;
            ErrorMessage = null;
            var envelope = await supportApi.CreateAsync(
                new CreateSupportTicketRequest(
                    SelectedKind.Value,
                    Title.Trim(),
                    Description.Trim()));

            if (envelope.Result is null)
            {
                ErrorMessage = envelope.ErrorMessage ?? "Не удалось отправить обращение.";
                return;
            }

            await Shell.Current.DisplayAlert(
                "Готово",
                "Ваше обращение отправлено. Ответ появится в разделе \"Мои обращения\".",
                "ОК");

            // Открываем ленту своих обращений — юзер сразу видит новую запись.
            await Shell.Current.GoToAsync("..");
            await Shell.Current.GoToAsync("support-mine");
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
            IsBusy = false;
            SubmitCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private async Task BackAsync() => await Shell.Current.GoToAsync("..");
}

public sealed record KindOption(string Display, string Value)
{
    public override string ToString() => Display;
}
