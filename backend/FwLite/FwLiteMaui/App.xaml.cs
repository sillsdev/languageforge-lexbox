using FwLiteShared.Projects;
using FwLiteShared.Services;
using Microsoft.Extensions.Logging;

namespace FwLiteMaui;

public partial class App : Application
{
    private readonly MainPage _mainPage;
    private readonly LexboxProjectService _lexboxProjectService;
    private readonly ILogger<App> _logger;

    public App(MainPage mainPage, IPreferencesService preferences, LexboxProjectService lexboxProjectService, ILogger<App> logger)
    {
        _mainPage = mainPage;
        _lexboxProjectService = lexboxProjectService;
        _logger = logger;
        var lastUrl = preferences.Get(nameof(PreferenceKey.AppLastUrl));
        if (lastUrl?.StartsWith('/') == true)
        {
            mainPage.StartPath = lastUrl;
        }
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var window = CreateWindow(_mainPage);
        // An OS-frozen app (e.g. Android Doze) can come back with its push listener down and no connectivity
        // transition to recover on; resume is when the user is watching, so recover immediately instead of
        // waiting for the periodic backstop.
        window.Resumed += (_, _) => _ = EnsureListenersAfterResume();
        return window;
    }

    private async Task EnsureListenersAfterResume()
    {
        try
        {
            await _lexboxProjectService.EnsureListenersForTrackedProjects(kickReconnecting: true);
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
