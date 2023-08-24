using Microsoft.Playwright;
using Testing.Browser.Util;

namespace Testing.Browser.Page;

public class AdminDashboardPage : AuthenticatedBasePage<AdminDashboardPage>
{
    public AdminDashboardPage(IPage page)
    : base(page, "/admin", page.Locator(".breadcrumbs :text('Admin Dashboard')"))
    {
    }

    public async Task<ProjectPage> OpenProject(string projectName, string projectCode)
    {
        var projectTable = Page.Locator("table").Nth(0);
        return await TaskUtil.WhenAllTakeSecond(
            projectTable.GetByRole(AriaRole.Link, new() { Name = projectName }).ClickAsync(),
            new ProjectPage(Page, projectName, projectCode).WaitFor());
    }
}
