using FwLiteShared.Services;

namespace FwLiteMaui;

public partial class App : Application
{
    private readonly MainPage _mainPage;

    public App(MainPage mainPage, IPreferencesService preferences)
    {
        _mainPage = mainPage;
        var lastUrl = preferences.Get(nameof(PreferenceKey.AppLastUrl));
        if (!string.IsNullOrEmpty(lastUrl))
        {
            mainPage.StartPath = lastUrl;
        }
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return CreateWindow(_mainPage);
    }

    public static Window CreateWindow(MainPage mainPage, int? width = null)
    {
        var window = new Window(mainPage)
        {
            Title = "FieldWorks Lite " + AppVersion.Version,

        };
        if (width is not null) window.Width = width.Value;
        return window;
    }
}
