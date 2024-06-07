using System.Net.Http.Headers;
using System.Security.Cryptography;
using LocalWebApp.Routes;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace LocalWebApp.Auth;

/// <summary>
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
    private readonly IPublicClientApplication _application;
    AuthenticationResult? _authResult;

    public AuthHelpers(LoggerAdapter logger,
        IHttpMessageHandlerFactory httpMessageHandlerFactory,
        IOptions<AuthConfig> options,
        LinkGenerator linkGenerator,
        OAuthService oAuthService,
        UrlContext urlContext,
        Uri authority)
    {
        _httpMessageHandlerFactory = httpMessageHandlerFactory;
        _oAuthService = oAuthService;
        _urlContext = urlContext;
        _authority = authority;
        (var hostUrl, _isRedirectHostGuess) = urlContext.GetUrl();
        _redirectHost = HostString.FromUriComponent(hostUrl);
        var redirectUri = linkGenerator.GetUriByRouteValues(AuthRoutes.CallbackRoute, new RouteValueDictionary(), hostUrl.Scheme, _redirectHost);
        var optionsValue = options.Value;
        //todo configure token cache as seen here
        //https://github.com/AzureAD/microsoft-authentication-extensions-for-dotnet/wiki/Cross-platform-Token-Cache
        _application = PublicClientApplicationBuilder.Create(optionsValue.ClientId)
            .WithExperimentalFeatures()
            .WithLogging(logger)
            .WithHttpClientFactory(new HttpClientFactoryAdapter(httpMessageHandlerFactory))
            .WithRedirectUri(redirectUri)
            .WithOidcAuthority(authority.ToString())
            .Build();
        _ = MsalCacheHelper.CreateAsync(BuildCacheProperties()).ContinueWith(
            task =>
            {
                var msalCacheHelper = task.Result;
                msalCacheHelper.RegisterCache(_application.UserTokenCache);
            }, scheduler: TaskScheduler.Default);
    }

    public static readonly KeyValuePair<string, string> LinuxKeyRingAttr1 = new("Version", "1");

    public static readonly KeyValuePair<string, string> LinuxKeyRingAttr2 = new("ProductGroup", "Lexbox");

    private static StorageCreationProperties BuildCacheProperties()
    {
        const string KeyChainServiceName = "lexbox_msal_service";
        const string KeyChainAccountName = "lexbox_msal_account";

        const string LinuxKeyRingSchema = "org.sil.lexbox.tokencache";
        const string LinuxKeyRingCollection = MsalCacheHelper.LinuxKeyRingDefaultCollection;
        const string LinuxKeyRingLabel = "MSAL token cache for Lexbox.";

        var propertiesBuilder = new StorageCreationPropertiesBuilder("msal.cache", Directory.GetCurrentDirectory());
#if DEBUG
        propertiesBuilder.WithUnprotectedFile();
#else
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

    public async Task<string> SignIn()
    {
        var authUri = await _oAuthService.SubmitLoginRequest(_application);
        return authUri.ToString();
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
        _authResult = await _application.AcquireTokenSilent(DefaultScopes, account).ExecuteAsync();
        return _authResult;
    }

    public async Task<string?> GetCurrentName()
    {
        var auth = await GetAuth();
        return auth?.Account.Username;
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
