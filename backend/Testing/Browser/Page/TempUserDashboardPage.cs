using Microsoft.Playwright;

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
        await Page.DeleteUser(User.Id);
    }
}
