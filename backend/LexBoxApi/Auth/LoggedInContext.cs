using LexCore.Auth;

namespace LexBoxApi.Auth;

public class LoggedInContext : IDisposable
{
    private readonly Lazy<LexAuthUser?> _user;

    public LoggedInContext(IHttpContextAccessor httpContextAccessor)
    {
        _user = new Lazy<LexAuthUser?>(() =>
        {
            var claimsPrincipal = httpContextAccessor.HttpContext?.User;
            if (claimsPrincipal is null) return null;
            var user = LexAuthUser.FromClaimsPrincipal(claimsPrincipal);
            return user;
        });
    }

    /// <summary>
    /// get the logged in user, will throw an exception if the user is not logged in
    /// </summary>
    public LexAuthUser User =>
        _disposed
            ? throw new ObjectDisposedException(nameof(LoggedInContext),
                "this context has been disposed because the request that created it has finished")
            : _user.Value ?? throw new Exception("User is not logged in");
    public LexAuthUser? MaybeUser => _user.Value;

    private bool _disposed;

    public void Dispose()
    {
        _disposed = true;
    }
}
