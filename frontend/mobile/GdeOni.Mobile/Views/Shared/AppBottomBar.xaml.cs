namespace GdeOni.Mobile.Views.Shared;

public partial class AppBottomBar : ContentView
{
    public static readonly BindableProperty SelectedTabProperty = BindableProperty.Create(
        nameof(SelectedTab),
        typeof(string),
        typeof(AppBottomBar),
        defaultValue: "tracked",
        propertyChanged: OnSelectedTabChanged);

    public string SelectedTab
    {
        get => (string)GetValue(SelectedTabProperty);
        set => SetValue(SelectedTabProperty, value);
    }

    public AppBottomBar()
    {
        InitializeComponent();
        ApplySelected("tracked");
    }

    private static void OnSelectedTabChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AppBottomBar bar && newValue is string route)
            bar.ApplySelected(route);
    }

    private void ApplySelected(string route)
    {
        var azure = (Color)Application.Current!.Resources["Azure"];
        var sky = (Color)Application.Current!.Resources["Sky"];
        var inactive = Color.FromArgb("#9AA8B5");
        var transparent = Colors.Transparent;

        TabTracked.TextColor = route == "tracked" ? azure : inactive;
        TabTracked.BackgroundColor = route == "tracked" ? sky : transparent;

        TabEvents.TextColor = route == "events" ? azure : inactive;
        TabEvents.BackgroundColor = route == "events" ? sky : transparent;

        TabRoute.TextColor = route == "route" ? azure : inactive;
        TabRoute.BackgroundColor = route == "route" ? sky : transparent;

        TabProfile.TextColor = route == "profile" ? azure : inactive;
        TabProfile.BackgroundColor = route == "profile" ? sky : transparent;
    }

    private async void OnTrackedTapped(object? sender, EventArgs e)
    {
        if (SelectedTab == "tracked") return;
        await Shell.Current.GoToAsync("//tracked");
    }

    private async void OnEventsTapped(object? sender, EventArgs e)
    {
        if (SelectedTab == "events") return;
        await Shell.Current.GoToAsync("//events");
    }

    private async void OnRouteTapped(object? sender, EventArgs e)
    {
        if (SelectedTab == "route") return;
        await Shell.Current.GoToAsync("//route");
    }

    private async void OnProfileTapped(object? sender, EventArgs e)
    {
        if (SelectedTab == "profile") return;
        await Shell.Current.GoToAsync("//profile");
    }
}
