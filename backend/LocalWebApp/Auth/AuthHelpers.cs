using System.Net.Http.Headers;
using System.Threading.Channels;
using System.Web;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace LocalWebApp.Auth;

public class AuthHelpers: BackgroundService
{
    private IPublicClientApplication _application;
    public IPublicClientApplication App => _application;

    public AuthHelpers(LoggerAdapter logger)
    {
        _application = PublicClientApplicationBuilder.Create("becf2856-0690-434b-b192-a4032b72067f")
            .WithExperimentalFeatures()
            .WithLogging(logger)
            .WithHttpClientFactory(HttpClientFactory.Instance)
            .WithRedirectUri("http://localhost:5173/api/auth/oauth-callback")
            .WithOidcAuthority("https://localhost:3000").Build();
    }

    private class HttpClientFactory : IMsalHttpClientFactory
    {
        public static readonly HttpClientFactory Instance = new();

        public HttpClient GetHttpClient()
        {
            var client = new HttpClient(new HttpClientHandler()
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true
            });

            return client;
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

    private Dictionary<string, AppWebUi> _webUis = new();
    private Channel<AppWebUi> _webUiChannel = Channel.CreateUnbounded<AppWebUi>();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var webUi in _webUiChannel.Reader.ReadAllAsync(stoppingToken))
        {
            var result = await App.AcquireTokenInteractive(["profile openid"])
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
        if (_result?.ExpiresOn < DateTimeOffset.UtcNow.AddMinutes(5))
        {
            return _result;
        }

        var accounts = await App.GetAccountsAsync();
        var account = accounts.FirstOrDefault();
        if (account is null) return null;
        _result = await App.AcquireTokenSilent(["profile openid"], account).ExecuteAsync();
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
        var client = HttpClientFactory.Instance.GetHttpClient();
        client.BaseAddress = new Uri("https://localhost:3000");
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
