using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
using LexCore.Auth;

namespace LexBoxApi.GraphQL;

[QueryType]
public class TestQueries
{
    private readonly LoggedInContext _loggedInContext;

    public TestQueries(LoggedInContext loggedInContext)
    {
        _loggedInContext = loggedInContext;
    }

    public record IsAdminResponse(bool Value);

    [AdminRequired]
    public IsAdminResponse IsAdmin()
    {
        return new IsAdminResponse(_loggedInContext.User.Role == UserRole.admin);
    }
}
