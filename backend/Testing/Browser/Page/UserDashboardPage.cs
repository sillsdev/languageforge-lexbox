using Microsoft.Playwright;
using Shouldly;
using Testing.Browser.Util;

namespace Testing.Browser.Page;

public class UserDashboardPage : AuthenticatedBasePage<UserDashboardPage>
{
    public UserDashboardPage(IPage page)
    : base(page, "/", page.GetByRole(AriaRole.Heading, new() { Name = "My Projects" }))
    {
    }

    public async Task<ProjectPage> OpenProject(string projectName, string projectCode)
    {
        var projectHeader = Page.GetByRole(AriaRole.Heading, new() { Name = projectName });
        var projectCard = Page.Locator(".card", new() { Has = projectHeader });
        projectCode.ShouldNotBeNullOrEmpty();
        return await TaskUtil.WhenAllTakeSecond(
            projectCard.ClickAsync(),
            new ProjectPage(Page, projectName, projectCode).WaitFor());
    }
}
