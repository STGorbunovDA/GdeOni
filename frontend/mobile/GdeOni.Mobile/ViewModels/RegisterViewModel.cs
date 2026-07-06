using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GdeOni.Mobile.Services.Auth;
using GdeOni.Mobile.Services.Subscriptions;

namespace GdeOni.Mobile.ViewModels;

public partial class RegisterViewModel(
    IAuthService authService,
    IPaywallChecker paywallChecker) : ObservableObject
{
    // E24. URL юридических документов. На MVP захардкожены — должны
    // совпадать с серверным appsettings.Legal.PrivacyPolicyUrl/TermsUrl.
    // Когда добавим ILegalApi — будем подтягивать с бэка через
    // GET /api/legal/privacy-policy (там Url возвращается в DTO).
    private const string PrivacyPolicyUrl = "https://gdeoni.ru/legal/privacy";
    private const string TermsOfUseUrl = "https://gdeoni.ru/legal/terms";

    [ObservableProperty]
    private string _email = "";

    [ObservableProperty]
    private string _userName = "";

    [ObservableProperty]
    private string _fullName = "";

    [ObservableProperty]
    private string _password = "";

    [ObservableProperty]
    private string _passwordConfirm = "";

    /// <summary>
    /// D19 age-gate. Минимальный возраст — 14 лет (Условия использования,
    /// п. 3.4). Инициализируем 18-летним значением по умолчанию, чтобы
    /// DatePicker показал разумную дату, а не 01.01.0001. Финальная
    /// проверка возраста — на бэке.
    /// </summary>
    [ObservableProperty]
    private DateTime _birthDate = DateTime.Today.AddYears(-18);

    /// <summary>
    /// E24. Чекбокс "Принимаю Privacy Policy" — обязателен для 152-ФЗ.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    private bool _privacyAccepted;

    /// <summary>
    /// E24. Чекбокс "Принимаю Terms of Use".
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    private bool _termsAccepted;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    [NotifyPropertyChangedFor(nameof(CanSubmit))]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool IsNotBusy => !IsBusy;
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    /// <summary>
    /// Кнопка "Зарегистрироваться" включается только когда оба чекбокса
    /// отмечены и идёт не-фоновое состояние. 152-ФЗ требует явного
    /// согласия — поэтому без них submit заблокирован.
    /// </summary>
    public bool CanSubmit => PrivacyAccepted && TermsAccepted && IsNotBusy;

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Email и пароль обязательны.";
            return;
        }

        if (Password != PasswordConfirm)
        {
            ErrorMessage = "Пароли не совпадают.";
            return;
        }

        if (!PrivacyAccepted || !TermsAccepted)
        {
            // На уровне UI кнопка disabled (CanSubmit), но дублируем на
            // случай, если команда дёрнется программно.
            ErrorMessage = "Чтобы зарегистрироваться, примите Privacy Policy и Terms of Use.";
            return;
        }

        // D19. Возрастной guard: 14+. Точную проверку делает бэк
        // (User.Register + TimeProvider); здесь просто предупредительный
        // клиентский фильтр, чтобы юзер не отправлял заведомо неверную
        // форму и получал разумный текст.
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var birth = DateOnly.FromDateTime(BirthDate);
        if (birth > today)
        {
            ErrorMessage = "Дата рождения не может быть в будущем.";
            return;
        }
        var age = today.Year - birth.Year;
        if (birth > today.AddYears(-age)) age--;
        if (age < 14)
        {
            ErrorMessage = "Сервисом могут пользоваться лица от 14 лет.";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = null;

            var result = await authService.RegisterAsync(
                Email.Trim(),
                string.IsNullOrWhiteSpace(UserName) ? null : UserName.Trim(),
                string.IsNullOrWhiteSpace(FullName) ? null : FullName.Trim(),
                Password,
                DateOnly.FromDateTime(BirthDate),
                PrivacyAccepted,
                TermsAccepted);

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "Не удалось зарегистрироваться.";
                return;
            }

            // RegisterAsync уже сделал auto-login и сохранил токены.
            // Бэк стартует Trial на 30 дней при Register, так что paywall
            // на новом юзере не должен сработать — но проверяем на всякий
            // случай (если SubscriptionEnabled=true и Trial по какой-то
            // причине не активировался).
            Password = "";
            PasswordConfirm = "";
            var target = await paywallChecker.ShouldShowPaywallAsync()
                ? "//subscription-required"
                : "//main/tracked";
            await Shell.Current.GoToAsync(target);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task OpenPrivacyPolicyAsync()
    {
        try
        {
            await Launcher.Default.OpenAsync(new Uri(PrivacyPolicyUrl));
        }
        catch
        {
            // URI hardcoded — кидать может только при отсутствии браузера.
        }
    }

    [RelayCommand]
    private async Task OpenTermsOfUseAsync()
    {
        try
        {
            await Launcher.Default.OpenAsync(new Uri(TermsOfUseUrl));
        }
        catch
        {
        }
    }

    [RelayCommand]
    private async Task BackToLoginAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
