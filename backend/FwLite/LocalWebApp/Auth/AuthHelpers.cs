using System.Net.Http.Headers;
using System.Security.Cryptography;
using LocalWebApp.Routes;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace LocalWebApp.Auth;

/// <summary>
/// when injected directly it will use the authority of the current project, to get a different authority use <see cref="AuthHelpersFactory"/>
/// helper class for using MSAL.net
/// docs: https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/overview
/// </summary>
public class AuthHelpers
{
    public static IReadOnlyCollection<string> DefaultScopes { get; } = ["profile", "openid"];
    public const string AuthHttpClientName = "AuthHttpClient";
    private readonly HostString _redirectHost;
    private readonly bool _isRedirectHostGuess;
    private readonly IHttpMessageHandlerFactory _httpMessageHandlerFactory;
    private readonly OAuthService _oAuthService;
    private readonly UrlContext _urlContext;
    private readonly Uri _authority;
    private readonly ILogger<AuthHelpers> _logger;
    private readonly IPublicClientApplication _application;
    AuthenticationResult? _authResult;

    public AuthHelpers(LoggerAdapter loggerAdapter,
        IHttpMessageHandlerFactory httpMessageHandlerFactory,
        IOptions<AuthConfig> options,
        LinkGenerator linkGenerator,
        OAuthService oAuthService,
        UrlContext urlContext,
        Uri authority,
        ILogger<AuthHelpers> logger,
        IHostEnvironment hostEnvironment)
    {
        _httpMessageHandlerFactory = httpMessageHandlerFactory;
        _oAuthService = oAuthService;
        _urlContext = urlContext;
        _authority = authority;
        _logger = logger;
        (var hostUrl, _isRedirectHostGuess) = urlContext.GetUrl();
        _redirectHost = HostString.FromUriComponent(hostUrl);
        var redirectUri = options.Value.SystemWebViewLogin
            ? "http://localhost" //system web view will always have no path, changing this will not do anything in that case
            : linkGenerator.GetUriByRouteValues(AuthRoutes.CallbackRoute,
                new RouteValueDictionary(),
                hostUrl.Scheme,
                _redirectHost);
        //todo configure token cache as seen here
        //https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/wiki/Cross-platform-Token-Cache
        _application = PublicClientApplicationBuilder.Create(options.Value.ClientId)
            .WithExperimentalFeatures()
            .WithLogging(loggerAdapter, hostEnvironment.IsDevelopment())
            .WithHttpClientFactory(new HttpClientFactoryAdapter(httpMessageHandlerFactory))
            .WithRedirectUri(redirectUri)
            .WithOidcAuthority(authority.ToString())
            .Build();
        _ = MsalCacheHelper.CreateAsync(BuildCacheProperties(options.Value.CacheFileName)).ContinueWith(
            task =>
            {
                var msalCacheHelper = task.Result;
                msalCacheHelper.RegisterCache(_application.UserTokenCache);
            },
            scheduler: TaskScheduler.Default);
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

    public bool IsHostUrlValid()
    {
        return !_isRedirectHostGuess || _redirectHost == HostString.FromUriComponent(_urlContext.GetUrl().host);
    }

    private class HttpClientFactoryAdapter(IHttpMessageHandlerFactory httpMessageHandlerFactory)
        : IMsalHttpClientFactory
    {
        public HttpClient GetHttpClient()
        {
            return new HttpClient(httpMessageHandlerFactory.CreateHandler(AuthHttpClientName), false);
        }
    }

    public async Task<OAuthService.SignInResult> SignIn(CancellationToken cancellation = default)
    {
        return await _oAuthService.SubmitLoginRequest(_application, cancellation);
    }

    public async Task Logout()
    {
        _authResult = null;
        var accounts = await _application.GetAccountsAsync();
        foreach (var account in accounts)
        {
            await _application.RemoveAsync(account);
        }
    }

    private async ValueTask<AuthenticationResult?> GetAuth()
    {
        if (DateTimeOffset.UtcNow.AddMinutes(5) < _authResult?.ExpiresOn)
        {
            return _authResult;
        }

        var accounts = await _application.GetAccountsAsync();
        var account = accounts.FirstOrDefault();
        if (account is null) return null;
        try
        {
            _authResult = await _application.AcquireTokenSilent(DefaultScopes, account).ExecuteAsync();
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

        return _authResult;
    }

    public async Task<string?> GetCurrentName()
    {
        var auth = await GetAuth();
        return auth?.Account.Username;
    }

    public async ValueTask<string?> GetCurrentToken()
    {
        var auth = await GetAuth();
        return auth?.AccessToken;
    }

    /// <summary>
    /// will return null if no auth token is available
    /// </summary>
    public async ValueTask<HttpClient?> CreateClient()
    {
        var auth = await GetAuth();
        if (auth is null) return null;

        var handler = _httpMessageHandlerFactory.CreateHandler(AuthHttpClientName);
        var client = new HttpClient(handler, false);
        client.BaseAddress = _authority;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }
}
