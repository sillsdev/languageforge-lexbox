using System.Net.Http.Headers;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace LocalWebApp.Auth;

public class AuthHelpers
{
    public static readonly AuthHelpers Instance = new();
    private PublicClientApplicationBuilder _appBuilder;
    private IPublicClientApplication _application;
    public IPublicClientApplication App => _application;

    private AuthHelpers()
    {
       _appBuilder = PublicClientApplicationBuilder.Create("becf2856-0690-434b-b192-a4032b72067f")
            .WithExperimentalFeatures()
            .WithHttpClientFactory(HttpClientFactory.Instance)
            .WithRedirectUri("http://localhost:9999/")
            // .WithClientSecret("wYzS8V6wfyCrBZERJVbJkgfcd464QBcEwJXZTNJBxD5k9HUc")
            .WithOidcAuthority("https://localhost:3000");
        _application = _appBuilder.Build();
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

    public async Task SignIn()
    {
        _result = await App.AcquireTokenInteractive([])
            // .WithCustomWebUi(new AppWebUi())
            .ExecuteAsync();
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
        _result = await App.AcquireTokenSilent([], account).ExecuteAsync();
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
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        return client;
    }

    private class AppWebUi : ICustomWebUi
    {
        public async Task<Uri> AcquireAuthorizationCodeAsync(Uri authorizationUri, Uri redirectUri, CancellationToken cancellationToken)
        {
            return null!;
        }
    }
}
