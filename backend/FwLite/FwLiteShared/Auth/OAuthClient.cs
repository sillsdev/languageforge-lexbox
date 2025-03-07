using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using FwLiteShared.Events;
using FwLiteShared.Projects;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace FwLiteShared.Auth;

/// <summary>
/// when injected directly it will use the authority of the current project, to get a different authority use <see cref="OAuthClientFactory"/>
/// helper class for using MSAL.net
/// docs: https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/overview
/// </summary>
public class OAuthClient
{
    //all 3 scopes are required to work around this bug https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/5094
    //more can be added in needed but this is the minimum set
    public static IReadOnlyCollection<string> DefaultScopes { get; } = ["profile", "openid", "offline_access", "sendandreceive" ];
    public const string AuthHttpClientName = "LexboxHttpClient";
    public string? RedirectUrl { get; }
    private readonly IHttpMessageHandlerFactory _httpMessageHandlerFactory;
    private readonly AuthConfig _options;
    private readonly OAuthService _oAuthService;
    private readonly LexboxServer _lexboxServer;
    private readonly LexboxProjectService _lexboxProjectService;
    private readonly ILogger<OAuthClient> _logger;
    private readonly GlobalEventBus _globalEventBus;
    private readonly IPublicClientApplication _application;
    private bool _cacheConfigured;
    private readonly SemaphoreSlim _cacheConfiguredSemaphore = new(1, 1);
    AuthenticationResult? _authResult;

    public OAuthClient(LoggerAdapter loggerAdapter,
        IHttpMessageHandlerFactory httpMessageHandlerFactory,
        IOptions<AuthConfig> options,
        OAuthService oAuthService,
        LexboxServer lexboxServer,
        LexboxProjectService lexboxProjectService,
        ILogger<OAuthClient> logger,
        GlobalEventBus globalEventBus,
        IHostEnvironment? hostEnvironment = null,
        IRedirectUrlProvider? redirectUrlProvider = null
            )
    {
        _httpMessageHandlerFactory = httpMessageHandlerFactory;
        _options = options.Value;
        _oAuthService = oAuthService;
        _lexboxServer = lexboxServer;
        _lexboxProjectService = lexboxProjectService;
        _logger = logger;
        _globalEventBus = globalEventBus;
        RedirectUrl = options.Value.SystemWebViewLogin
            ? "http://localhost" //system web view will always have no path, changing this will not do anything in that case
            :  redirectUrlProvider?.GetRedirectUrl() ?? throw new InvalidOperationException("No IRedirectUrlProvider configured, required for non-system web view login");
        //todo configure token cache as seen here
        //https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/wiki/Cross-platform-Token-Cache
        var builder = PublicClientApplicationBuilder.Create(options.Value.ClientId)
            .WithExperimentalFeatures()
            .WithLogging(loggerAdapter, hostEnvironment?.IsDevelopment() ?? false)
            .WithHttpClientFactory(new HttpClientFactoryAdapter(httpMessageHandlerFactory))
            .WithParentActivityOrWindow(() => options.Value.ParentActivityOrWindow)
            .WithOidcAuthority(lexboxServer.Authority.ToString());
        if (!options.Value.SystemWebViewLogin)
        {
            builder.WithRedirectUri(RedirectUrl);
        }
        else
        {
            builder.WithDefaultRedirectUri();
        }
        _application = builder.Build();
    }

    public static readonly KeyValuePair<string, string> LinuxKeyRingAttr1 = new("Version", "1");
    public static readonly KeyValuePair<string, string> LinuxKeyRingAttr2 = new("ProductGroup", "Lexbox");

    private static StorageCreationProperties BuildCacheProperties(string cacheFileName)
    {
        if (!Path.IsPathFullyQualified(cacheFileName))
            throw new ArgumentException("Cache file name must be fully qualified");
        var propertiesBuilder =
            new StorageCreationPropertiesBuilder(cacheFileName, Path.GetDirectoryName(cacheFileName));
#if DEBUG
        propertiesBuilder.WithUnprotectedFile();
#else
        const string KeyChainServiceName = "lexbox_msal_service";
        const string KeyChainAccountName = "lexbox_msal_account";

        const string LinuxKeyRingSchema = "org.sil.lexbox.tokencache";
        const string LinuxKeyRingCollection = MsalCacheHelper.LinuxKeyRingDefaultCollection;
        const string LinuxKeyRingLabel = "MSAL token cache for Lexbox.";
        propertiesBuilder.WithLinuxKeyring(LinuxKeyRingSchema,
                LinuxKeyRingCollection,
                LinuxKeyRingLabel,
                LinuxKeyRingAttr1,
                LinuxKeyRingAttr2)
            .WithMacKeyChain(KeyChainServiceName, KeyChainAccountName);
#endif
        return propertiesBuilder.Build();
    }

    private async ValueTask ConfigureCache()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return;
        if (_cacheConfigured) return;
        try
        {
            await _cacheConfiguredSemaphore.WaitAsync();
            if (_cacheConfigured) return;
            var cacheHelper = await MsalCacheHelper.CreateAsync(BuildCacheProperties(_options.CacheFileName));
            cacheHelper.RegisterCache(_application.UserTokenCache);
            _cacheConfigured = true;
        }
        finally
        {
            _cacheConfiguredSemaphore.Release();
        }

    }

    private class HttpClientFactoryAdapter(IHttpMessageHandlerFactory httpMessageHandlerFactory)
        : IMsalHttpClientFactory
    {
        public HttpClient GetHttpClient()
        {
            return new HttpClient(httpMessageHandlerFactory.CreateHandler(AuthHttpClientName), false);
        }
    }

    public async Task<OAuthService.SignInResult> SignIn(string returnUrl, CancellationToken cancellation = default)
    {
        await ConfigureCache();
        return await _oAuthService.SubmitLoginRequest(_application, returnUrl, _lexboxServer, cancellation);
    }

    public async Task Logout()
    {
        _authResult = null;
        await ConfigureCache();
        var accounts = await _application.GetAccountsAsync();
        foreach (var account in accounts)
        {
            await _application.RemoveAsync(account);
        }
        _globalEventBus.PublishEvent(new AuthenticationChangedEvent(_lexboxServer));
    }

    private async ValueTask<AuthenticationResult?> GetAuth(bool forceRefresh = false)
    {
        if (DateTimeOffset.UtcNow.AddMinutes(5) < _authResult?.ExpiresOn && !forceRefresh)
        {
            return _authResult;
        }

        await ConfigureCache();
        var accounts = await _application.GetAccountsAsync();
        var account = accounts.FirstOrDefault();
        if (account is null) return null;
        try
        {
            _authResult = await _application.AcquireTokenSilent(DefaultScopes, account)
                .WithForceRefresh(forceRefresh)
                .ExecuteAsync();
        }
        catch (MsalUiRequiredException)
        {
            _logger.LogWarning("Ui required, logging out");
            await _application.RemoveAsync(account);
            _authResult = null;
        }
        catch (MsalClientException e) when (e.ErrorCode == "multiple_matching_tokens_detected")
        {
            _logger.LogWarning(e, "Multiple matching tokens detected, logging out");
            await _application.RemoveAsync(account);
            _authResult = null;
        }
        catch (MsalServiceException e) when (e.InnerException is HttpRequestException)
        {
            _logger.LogWarning(e, "Failed to acquire token silently");
            await _application
                .RemoveAsync(account); //todo might not be the best way to handle this, maybe it's a transient error?
            _authResult = null;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to acquire token silently");
            await _application.RemoveAsync(account);
            _authResult = null;
        }

        return _authResult;
    }

    public async Task<string?> GetCurrentName()
    {
        var auth = await GetAuth();
        return auth?.Account.Username;
    }

    public async Task<LexboxUser?> GetCurrentUser()
    {
        var auth = await GetAuth();
        return auth?.Account.Username is null ? null : new LexboxUser(auth.Account.Username, auth.Account.HomeAccountId.ObjectId);
    }

    public async ValueTask<string?> GetCurrentToken()
    {
        var auth = await GetAuth();
        return auth?.AccessToken;
    }

    /// <summary>
    /// will return null if no auth token is available
    /// </summary>
    public async ValueTask<HttpClient?> CreateHttpClient()
    {
        var auth = await GetAuth();
        if (auth is null) return null;

        var handler = _httpMessageHandlerFactory.CreateHandler(AuthHttpClientName);
        var client = new HttpClient(handler, false);
        client.BaseAddress = _lexboxServer.Authority;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    public async Task RefreshToken()
    {
        var auth = await GetAuth(true);
        if (auth is null)
        {
            _logger.LogWarning("Unable to refresh token");
        }
        else
        {
            _logger.LogInformation("Refreshed token");
        }
    }
}
