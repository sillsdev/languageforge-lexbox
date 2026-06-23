using FwLiteShared.Projects;
using FwLiteShared.Services;
using Microsoft.Extensions.Logging;

namespace FwLiteMaui;

public partial class App : Application
{
    public IServiceProvider ServiceProvider { get; }
    private readonly MainPage _mainPage;
    public static string? OverrideStartupUrl { get; set; }
    private readonly LexboxProjectChangeListener _lexboxProjectChangeListener;
    private readonly ILogger<App> _logger;

    public App(MainPage mainPage, IPreferencesService preferences, LexboxProjectChangeListener lexboxProjectChangeListener, ILogger<App> logger, IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
        _mainPage = mainPage;
        _lexboxProjectChangeListener = lexboxProjectChangeListener;
        _logger = logger;
        var lastUrl = preferences.Get(nameof(PreferenceKey.AppLastUrl));
        if (lastUrl?.StartsWith('/') == true)
        {
            mainPage.StartPath = lastUrl;
        }
        InitializeComponent();
    }

    internal void LoadAppUrl(string url)
    {
        _mainPage.LoadAppUrl(url);
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = CreateWindow(_mainPage);
        // An OS-frozen app (e.g. Android Doze/Standby) can come back with its push listener down and no connectivity
        // transition to recover on; resume is when the user is watching, so recover immediately instead of
        // waiting for the periodic backstop.
        window.Resumed += (_, _) => _ = EnsureListenersAfterResume();
        return window;
    }

    private async Task EnsureListenersAfterResume()
    {
        try
        {
            _logger.LogInformation("App resumed; ensuring push listeners");
            await _lexboxProjectChangeListener.EnsureListenersForTrackedProjects(kickReconnecting: true);
        }
        catch (Exception e)
        {
            _logger.LogWarning(e, "Failed to ensure push listeners after app resume");
        }
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
