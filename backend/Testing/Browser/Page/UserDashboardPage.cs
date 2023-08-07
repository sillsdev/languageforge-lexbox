using Microsoft.Playwright;

namespace Testing.Browser.Page;

public class UserDashboardPage : AuthenticatedBasePage<UserDashboardPage>
{
    public UserDashboardPage(IPage page)
    : base(page, "/", page.GetByRole(AriaRole.Heading, new() { Name = "My Projects" }))
    {
    }
}
