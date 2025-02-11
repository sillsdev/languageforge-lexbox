using System.Threading.Channels;
using System.Web;
using FwLiteShared.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace FwLiteShared.Auth;

//this class is commented with a number of step comments, these are the steps in the OAuth flow
//if a step comes before a method that means it awaits that call, if it comes after that means it resumes after the above await
public class OAuthService(
    ILogger<OAuthService> logger,
    IOptions<AuthConfig> options,
    GlobalEventBus globalEventBus,
    IHostApplicationLifetime? applicationLifetime = null) : BackgroundService
{
    public record SignInResult(Uri? AuthUri, bool HandledBySystemWebView);

    public async Task<SignInResult> SubmitLoginRequest(IPublicClientApplication application,
        string returnUrl,
        LexboxServer lexboxServer,
        CancellationToken cancellation)
    {
        if (options.Value.SystemWebViewLogin)
        {
            await HandleSystemWebViewLogin(application, cancellation);
            globalEventBus.PublishEvent(new AuthenticationChangedEvent(lexboxServer.Id));
            return new(null, true);
        }

        var request = new OAuthLoginRequest(application, returnUrl, lexboxServer);
        if (!_requestChannel.Writer.TryWrite(request))
        {
            throw new InvalidOperationException("Only one request at a time");
        }

        //step 1
        var uri = await request.GetAuthUri(applicationLifetime?.ApplicationStopping.Merge(cancellation) ??
                                           cancellation);
        //step 4
        if (request.State is null) throw new InvalidOperationException("State is null");
        _oAuthLoginRequests[request.State] = request;
        return new(uri, false);
    }

    private async Task HandleSystemWebViewLogin(IPublicClientApplication application, CancellationToken cancellation)
    {
        var result = await application.AcquireTokenInteractive(OAuthClient.DefaultScopes)
            .WithUseEmbeddedWebView(false)
            .WithParentActivityOrWindow(options.Value.ParentActivityOrWindow)
            .WithSystemWebViewOptions(new() { })
            .ExecuteAsync(cancellation);
    }

    public async Task<(AuthenticationResult, string ClientReturnUrl)> FinishLoginRequest(Uri uri,
        CancellationToken cancellation = default)
    {
        var queryString = HttpUtility.ParseQueryString(uri.Query);
        var state = queryString.Get("state") ?? throw new InvalidOperationException("State is null");
        if (!_oAuthLoginRequests.TryGetValue(state, out var request))
            throw new InvalidOperationException("Invalid state");
        //step 5
        request.SetReturnUri(uri);
        var result = (
            await request.GetAuthenticationResult(applicationLifetime?.ApplicationStopping.Merge(cancellation) ??
                                                  cancellation),
            request.ClientReturnUrl);
        globalEventBus.PublishEvent(new AuthenticationChangedEvent(request.LexboxServer.Id));
        return result;
        //step 8
    }

    private readonly Dictionary<string, OAuthLoginRequest> _oAuthLoginRequests = new();

    private readonly Channel<OAuthLoginRequest>
        _requestChannel = Channel.CreateBounded<OAuthLoginRequest>(1); //only one request at a time

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var loginRequest in _requestChannel.Reader.ReadAllAsync(stoppingToken))
            {
                //don't await, otherwise we'll block the channel reader and only 1 login will be processed at a time
                //cancel the login after 5 minutes, otherwise it'll probably hang forever and abandoned requests will never be cleaned up
                _ = Task.Run(() => StartLogin(loginRequest, stoppingToken.Merge(new CancellationTokenSource(TimeSpan.FromMinutes(5)).Token)), stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected during shutdown
        }
    }

    private async Task StartLogin(OAuthLoginRequest loginRequest, CancellationToken stoppingToken)
    {
        //this sits here and waits for AcquireAuthorizationCodeAsync to finish, meanwhile the uri passed in to that method is sent back to the caller of SubmitLoginRequest
        //which then redirects the browser to that uri, once it's done it's sent back and calls FinishLoginRequest, which sends it's uri to OAuthLoginRequest
        //which causes AcquireAuthorizationCodeAsync to return
        try
        {
            //step 2
            var result = await loginRequest.Application.AcquireTokenInteractive(OAuthClient.DefaultScopes)
                .WithCustomWebUi(loginRequest)
                .ExecuteAsync(stoppingToken);
            //step 7, causes step 8 to resume
            loginRequest.SetAuthenticationResult(result);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error getting token");
            loginRequest.SetException(e);
        }

        if (loginRequest.State is not null)
            _oAuthLoginRequests.Remove(loginRequest.State);
    }
}

/// <summary>
/// this is a bit of a hack. the MSAL library expects to be running in a native app which opens a browser and waits for a response URL to come back
/// instead we have to do this so we can use the currently open browser, redirect it to the auth url passed in here and then once it's done and the callback comes to our server,
/// send that  call to here so that MSAL can pull out the access token
/// </summary>
public class OAuthLoginRequest(IPublicClientApplication app, string clientReturnUrl, LexboxServer lexboxServer) : ICustomWebUi
{
    public IPublicClientApplication Application { get; } = app;
    public string? State { get; private set; }
    private readonly TaskCompletionSource<Uri> _authUriTcs = new();
    private readonly TaskCompletionSource<Uri> _returnUriTcs = new();
    private readonly TaskCompletionSource<AuthenticationResult> _resultTcs = new();

    public async Task<Uri> AcquireAuthorizationCodeAsync(Uri authorizationUri,
        Uri redirectUri,
        CancellationToken cancellationToken)
    {
        cancellationToken.Register(_resultTcs.SetCanceled);
        State = HttpUtility.ParseQueryString(authorizationUri.Query).Get("state");
        //triggers step 1 to finish awaiting
        _authUriTcs.SetResult(authorizationUri);

        //step 3
        return await _returnUriTcs.Task.WaitAsync(cancellationToken);
        //step 6
    }

    public Task<Uri> GetAuthUri(CancellationToken cancellation) => _authUriTcs.Task.WaitAsync(cancellation);
    public void SetReturnUri(Uri uri) => _returnUriTcs.SetResult(uri);
    public void SetAuthenticationResult(AuthenticationResult result) => _resultTcs.SetResult(result);

    public void SetException(Exception e)
    {
        if (_authUriTcs.Task.IsCompleted)
            _resultTcs.SetException(e);
        else
            _authUriTcs.SetException(e);
    }

    public Task<AuthenticationResult> GetAuthenticationResult(CancellationToken cancellation) =>
        _resultTcs.Task.WaitAsync(cancellation);

    /// <summary>
    /// url to return the client to once the login is finished
    /// </summary>
    public string ClientReturnUrl { get; } = clientReturnUrl;

    public LexboxServer LexboxServer { get; } = lexboxServer;
}
