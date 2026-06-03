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
    //profile, openid and offline_access are all required to work around https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues/5094;
    //sendandreceive is the API permission scope for our backend.
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
    //serializes GetAuth so concurrent callers don't both redeem the same refresh token — rolling-RT replay
    //detection on the server would otherwise revoke the whole chain and force a real logout.
    private readonly SemaphoreSlim _authSemaphore = new(1, 1);
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

    //test seam — skips persistent cache wiring; only GetAuth/Logout are safe to exercise via this ctor.
    internal OAuthClient(IPublicClientApplication application,
        LexboxServer lexboxServer,
        ILogger<OAuthClient> logger,
        GlobalEventBus globalEventBus)
    {
        _application = application;
        _lexboxServer = lexboxServer;
        _logger = logger;
        _globalEventBus = globalEventBus;
        _cacheConfigured = true;

        _httpMessageHandlerFactory = null!;
        _options = null!;
        _oAuthService = null!;
        _lexboxProjectService = null!;
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
        //take the same lock as GetAuth: otherwise an in-flight AcquireTokenSilent could complete after we
        //clear state and re-populate _authResult, leaving the user silently logged back in.
        await _authSemaphore.WaitAsync();
        try
        {
            _authResult = null;
            await ConfigureCache();
            var accounts = await _application.GetAccountsAsync();
            foreach (var account in accounts)
            {
                await _application.RemoveAsync(account);
            }
        }
        finally
        {
            _authSemaphore.Release();
        }
        //publish outside the lock: Subject.OnNext dispatches synchronously, so a subscriber that
        //re-enters a GetAuth/Logout path would self-deadlock on the non-reentrant _authSemaphore.
        _globalEventBus.PublishEvent(new AuthenticationChangedEvent(_lexboxServer));
    }

    /// <summary>
    /// Whether an account is present in the local MSAL cache. Purely a local read — never touches
    /// the network — so callers can distinguish "not logged in" from "offline" without triggering a
    /// token acquisition. Account presence implies signed-in: Logout and RemoveAccountAsync are the
    /// only paths that remove an account.
    /// </summary>
    public async ValueTask<bool> IsSignedIn()
    {
        await ConfigureCache();
        var accounts = await _application.GetAccountsAsync();
        return accounts.Any();
    }

    internal async ValueTask<AuthenticationResult?> GetAuth(bool forceRefresh = false)
    {
        if (DateTimeOffset.UtcNow.AddMinutes(5) < _authResult?.ExpiresOn && !forceRefresh)
        {
            return _authResult;
        }

        var accountRemoved = false;
        AuthenticationResult? result;
        await _authSemaphore.WaitAsync();
        try
        {
            //re-check inside the lock: another caller may have refreshed while we waited
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
                _authResult = await AcquireTokenSilentAsync(account, forceRefresh);
            }
            catch (Exception e)
            {
                switch (ClassifySilentAuthFailure(e))
                {
                    case SilentAuthFailureOutcome.RemoveAccount:
                        _logger.LogWarning(e, "Silent token acquisition failed with a non-recoverable error, logging out");
                        await RemoveAccountAsync(account);
                        accountRemoved = true;
                        break;
                    case SilentAuthFailureOutcome.KeepCachedCredentials:
                        _logger.LogWarning(e, "Silent token acquisition failed with a transient or unknown error; keeping cached credentials");
                        // Only drop the in-memory access token if it has actually expired. Keeping a still-valid
                        // token across a transient blip avoids spuriously taking SignalR offline in the 5-minute
                        // pre-expiry refresh window. The refresh token in the MSAL cache is preserved either way,
                        // so the next GetAuth still retries silently once the transient condition clears.
                        if (_authResult is { } r && r.ExpiresOn <= DateTimeOffset.UtcNow)
                        {
                            _authResult = null;
                        }
                        break;
                }
            }

            //capture under the lock: after release a concurrent GetAuth could repopulate _authResult.
            result = _authResult;
        }
        finally
        {
            _authSemaphore.Release();
        }

        //publish outside the lock, as Logout does (see the rationale there).
        if (accountRemoved) _globalEventBus.PublishEvent(new AuthenticationChangedEvent(_lexboxServer));
        return result;
    }

    //test seam — overridable so tests can stub the result without faking MSAL's builder chain.
    internal virtual Task<AuthenticationResult> AcquireTokenSilentAsync(IAccount account, bool forceRefresh)
    {
        return _application.AcquireTokenSilent(DefaultScopes, account)
            .WithForceRefresh(forceRefresh)
            .ExecuteAsync();
    }

    internal enum SilentAuthFailureOutcome
    {
        KeepCachedCredentials,
        RemoveAccount,
    }

    //KeepCachedCredentials is the default so a transient network error doesn't wipe the refresh token.
    //GetAuth takes no CancellationToken, so any OperationCanceledException is MSAL-internal (an HttpClient
    //timeout surfacing as TaskCanceledException on a flaky network) and is therefore transient.
    internal static SilentAuthFailureOutcome ClassifySilentAuthFailure(Exception e) => e switch
    {
        MsalUiRequiredException => SilentAuthFailureOutcome.RemoveAccount,
        MsalClientException { ErrorCode: "multiple_matching_tokens_detected" } => SilentAuthFailureOutcome.RemoveAccount,
        _ => SilentAuthFailureOutcome.KeepCachedCredentials,
    };

    //caller is responsible for publishing AuthenticationChangedEvent after releasing _authSemaphore (see Logout).
    private async Task RemoveAccountAsync(IAccount account)
    {
        await _application.RemoveAsync(account);
        _authResult = null;
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
