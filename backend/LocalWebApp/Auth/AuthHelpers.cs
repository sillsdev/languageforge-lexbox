using System.Net.Http.Headers;
using System.Threading.Channels;
using System.Web;
using LocalWebApp.Routes;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace LocalWebApp.Auth;

public class AuthHelpers: BackgroundService
{
    public const string AuthHttpClientName = "AuthHttpClient";
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<AuthConfig> _options;
    private Lazy<IPublicClientApplication> _lazyApp;
    private IPublicClientApplication _application => _lazyApp.Value;


    public AuthHelpers(LoggerAdapter logger, IHttpClientFactory httpClientFactory, IOptions<AuthConfig> options, LinkGenerator linkGenerator, IHttpContextAccessor accessor)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
        _lazyApp = new(() =>
        {
            var httpContext = accessor.HttpContext;
            ArgumentNullException.ThrowIfNull(httpContext);
            var redirectUri = linkGenerator.GetUriByRouteValues(httpContext, AuthRoutes.CallbackRoute);
            var optionsValue = _options.Value;
            return PublicClientApplicationBuilder.Create(optionsValue.ClientId)
                .WithExperimentalFeatures()
                .WithLogging(logger)
                .WithHttpClientFactory(new HttpClientFactoryAdapter(httpClientFactory))
                .WithRedirectUri(redirectUri)
                .WithOidcAuthority(optionsValue.DefaultAuthority.ToString())
                .Build();
        });
    }

    private class HttpClientFactoryAdapter(IHttpClientFactory httpClientFactory) : IMsalHttpClientFactory
    {
        public HttpClient GetHttpClient()
        {
            return httpClientFactory.CreateClient(AuthHttpClientName);
        }
    }

    public async Task<string> SignIn()
    {
        var appWebUi = new AppWebUi();
        await _webUiChannel.Writer.WriteAsync(appWebUi);
        var authUri = await appWebUi.GetAuthUri();
        if (appWebUi.State is null) throw new InvalidOperationException("State is null");
        _webUis[appWebUi.State] = appWebUi;
        return authUri.ToString();
    }

    public async Task Logout()
    {
        _result = null;
        var accounts = await _application.GetAccountsAsync();
        foreach (var account in accounts)
        {
            await _application.RemoveAsync(account);
        }
    }

    public async Task<AuthenticationResult?> FinishSignin(Uri uri)
    {
        var queryString = HttpUtility.ParseQueryString(uri.Query);
        var state = queryString.Get("state") ?? throw new InvalidOperationException("State is null");
        if (_webUis.GetValueOrDefault(state) is { } appWebUi)
        {
            appWebUi.SetReturnUri(uri);
            return await appWebUi.GetAuthenticationResult();
        }

        return null;
    }

    private readonly Dictionary<string, AppWebUi> _webUis = new();
    private readonly Channel<AppWebUi> _webUiChannel = Channel.CreateUnbounded<AppWebUi>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var webUi in _webUiChannel.Reader.ReadAllAsync(stoppingToken))
        {
            var result = await _application.AcquireTokenInteractive(["profile openid"])
                .WithCustomWebUi((webUi))
                .ExecuteAsync(stoppingToken);
            webUi.SetAuthenticationResult(result);
            _result = result;
            if (webUi.State is not null)
                _webUis.Remove(webUi.State);
        }
    }

    AuthenticationResult? _result;

    private async Task<AuthenticationResult?> GetAuth()
    {
        if (DateTimeOffset.UtcNow.AddMinutes(5) < _result?.ExpiresOn)
        {
            return _result;
        }

        var accounts = await _application.GetAccountsAsync();
        var account = accounts.FirstOrDefault();
        if (account is null) return null;
        _result = await _application.AcquireTokenSilent(["profile openid"], account).ExecuteAsync();
        return _result;
    }

    public async Task<string?> GetCurrentName()
    {
        var auth = await GetAuth();
        return auth?.Account.Username;
    }

    public async Task<HttpClient?> CreateClient()
    {
        var auth = await GetAuth();
        if (auth is null) return null;

        var client = _httpClientFactory.CreateClient(AuthHttpClientName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    /// <summary>
    /// this is a bit of a hack. the MSAL library expects to be running in a native app which opens a browser and waits for a response URL to come back
    /// instead we have to do this so we can use the currently open browser, redirect it to the auth url passed in here and then once it's done and the callback comes to our server,
    /// send that  call to here so that MSAL can pull out the access token
    /// </summary>
    private class AppWebUi() : ICustomWebUi
    {
        public string? State { get; private set; }
        private readonly TaskCompletionSource<Uri> _authUriTcs = new();
        private readonly TaskCompletionSource<Uri> _returnUriTcs = new();
        private readonly TaskCompletionSource<AuthenticationResult> _resultTcs = new();

        public async Task<Uri> AcquireAuthorizationCodeAsync(Uri authorizationUri,
            Uri redirectUri,
            CancellationToken cancellationToken)
        {
            State = HttpUtility.ParseQueryString(authorizationUri.Query).Get("state");
            _authUriTcs.SetResult(authorizationUri);
            return await _returnUriTcs.Task.WaitAsync(cancellationToken);
        }

        public async Task<Uri> GetAuthUri() => await _authUriTcs.Task;
        public void SetReturnUri(Uri uri) => _returnUriTcs.SetResult(uri);
        public void SetAuthenticationResult(AuthenticationResult result) => _resultTcs.SetResult(result);
        public Task<AuthenticationResult> GetAuthenticationResult() => _resultTcs.Task;
    }
}
