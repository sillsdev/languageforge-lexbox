using Microsoft.Playwright;
using Testing.Browser.Util;
using Testing.Services;

namespace Testing.Browser.Page;

public class TempUserDashboardPage : UserDashboardPage, IAsyncDisposable
{
    public TempUser User { get; }

    public TempUserDashboardPage(IPage page, TempUser user) : base(page)
    {
        User = user;
    }

    public async ValueTask DisposeAsync()
    {
        var context = await Page.Context.Browser.NewContextAsync();
        await context.APIRequest.LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);
        await context.APIRequest.DeleteUser(User.Id);
    }
}
