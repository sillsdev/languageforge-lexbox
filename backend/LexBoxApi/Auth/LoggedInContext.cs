using LexCore.Auth;

namespace LexBoxApi.Auth;

public class LoggedInContext
{
    private readonly Lazy<LexAuthUser> _user;

    public LoggedInContext(IHttpContextAccessor httpContextAccessor)
    {
        _user = new Lazy<LexAuthUser>(() =>
        {
            var claimsPrincipal = httpContextAccessor.HttpContext?.User;
            if (claimsPrincipal is null) throw new Exception("User is not logged in");
            var user = LexAuthUser.FromClaimsPrincipal(claimsPrincipal);
            if (user is null) throw new Exception("User is not logged in");
            return user;
        });
    }

    public LexAuthUser User => _user.Value;
}