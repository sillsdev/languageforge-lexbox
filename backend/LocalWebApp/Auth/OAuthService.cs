using System.Threading.Channels;
using System.Web;
using LocalWebApp.Utils;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace LocalWebApp.Auth;

//this class is commented with a number of step comments, these are the steps in the OAuth flow
//if a step comes before a method that means it awaits that call, if it comes after that means it resumes after the above await
public class OAuthService(ILogger<OAuthService> logger, IHostApplicationLifetime applicationLifetime) : BackgroundService
{
    public async Task<Uri> SubmitLoginRequest(IPublicClientApplication application, CancellationToken cancellation)
    {
        var request = new OAuthLoginRequest(application);
        if (!_requestChannel.Writer.TryWrite(request))
        {
            throw new InvalidOperationException("Only one request at a time");
        }
        //step 1
        var uri = await request.GetAuthUri(applicationLifetime.ApplicationStopping.Merge(cancellation));
        //step 4
        if (request.State is null) throw new InvalidOperationException("State is null");
        _oAuthLoginRequests[request.State] = request;
        return uri;
    }

    public async Task<AuthenticationResult> FinishLoginRequest(Uri uri, CancellationToken cancellation = default)
    {
        var queryString = HttpUtility.ParseQueryString(uri.Query);
        var state = queryString.Get("state") ?? throw new InvalidOperationException("State is null");
        if (!_oAuthLoginRequests.TryGetValue(state, out var request))
            throw new InvalidOperationException("Invalid state");
        //step 5
        request.SetReturnUri(uri);
        return await request.GetAuthenticationResult(applicationLifetime.ApplicationStopping.Merge(cancellation));
        //step 8
    }

    private readonly Dictionary<string, OAuthLoginRequest> _oAuthLoginRequests = new();
    private readonly Channel<OAuthLoginRequest> _requestChannel = Channel.CreateBounded<OAuthLoginRequest>(1);//only one request at a time

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var loginRequest in _requestChannel.Reader.ReadAllAsync(stoppingToken))
        {
            //this sits here and waits for AcquireAuthorizationCodeAsync to finish, meanwhile the uri passed in to that method is sent back to the caller of SubmitLoginRequest
            //which then redirects the browser to that uri, once it's done it's sent back and calls FinishLoginRequest, which sends it's uri to OAuthLoginRequest
            //which causes AcquireAuthorizationCodeAsync to return

            try
            {
                //todo we can get stuck here if the user doesn't complete the login, this basically bricks the login at the moment. We need a timeout or something
                //step 2
                var result = await loginRequest.Application.AcquireTokenInteractive(AuthHelpers.DefaultScopes)
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
}

/// <summary>
/// this is a bit of a hack. the MSAL library expects to be running in a native app which opens a browser and waits for a response URL to come back
/// instead we have to do this so we can use the currently open browser, redirect it to the auth url passed in here and then once it's done and the callback comes to our server,
/// send that  call to here so that MSAL can pull out the access token
/// </summary>
public class OAuthLoginRequest(IPublicClientApplication app) : ICustomWebUi
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

    public Task<AuthenticationResult> GetAuthenticationResult(CancellationToken cancellation) => _resultTcs.Task.WaitAsync(cancellation);
}
