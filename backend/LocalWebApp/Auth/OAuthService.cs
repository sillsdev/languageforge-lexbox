using System.Threading.Channels;
using System.Web;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensibility;

namespace LocalWebApp.Auth;

//this class is commented with a number of step comments, these are the steps in the OAuth flow
//if a step comes before a method that means it awaits that call, if it comes after that means it resumes after the above await
public class OAuthService : BackgroundService
{
    public async Task<Uri> SubmitLoginRequest(IPublicClientApplication application)
    {
        var request = new OAuthLoginRequest(application);
        if (!_requestChannel.Writer.TryWrite(request))
        {
            throw new InvalidOperationException("Only one request at a time");
        }

        //step 1
        var uri = await request.GetAuthUri();
        //step 4
        if (request.State is null) throw new InvalidOperationException("State is null");
        _oAuthLoginRequests[request.State] = request;
        return uri;
    }

    public async Task<AuthenticationResult> FinishLoginRequest(Uri uri)
    {
        var queryString = HttpUtility.ParseQueryString(uri.Query);
        var state = queryString.Get("state") ?? throw new InvalidOperationException("State is null");
        if (!_oAuthLoginRequests.TryGetValue(state, out var request))
            throw new InvalidOperationException("Invalid state");
        //step 5
        request.SetReturnUri(uri);
        return await request.GetAuthenticationResult();
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

            //step 2
            var result = await loginRequest.Application.AcquireTokenInteractive(AuthHelpers.DefaultScopes)
                .WithCustomWebUi(loginRequest)
                .ExecuteAsync(stoppingToken);
            //step 7, causes step 8 to resume
            loginRequest.SetAuthenticationResult(result);
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
        State = HttpUtility.ParseQueryString(authorizationUri.Query).Get("state");
        //triggers step 1 to finish awaiting
        _authUriTcs.SetResult(authorizationUri);

        //step 3
        return await _returnUriTcs.Task.WaitAsync(cancellationToken);
        //step 6
    }

    public async Task<Uri> GetAuthUri() => await _authUriTcs.Task;
    public void SetReturnUri(Uri uri) => _returnUriTcs.SetResult(uri);
    public void SetAuthenticationResult(AuthenticationResult result) => _resultTcs.SetResult(result);
    public Task<AuthenticationResult> GetAuthenticationResult() => _resultTcs.Task;
}
