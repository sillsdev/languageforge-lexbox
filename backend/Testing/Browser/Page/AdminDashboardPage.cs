using Microsoft.Playwright;

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
        await projectTable.GetByRole(AriaRole.Link, new() { Name = projectName, Exact = true}).ClickAsync();
        return await new ProjectPage(Page, projectName, projectCode).WaitFor();
    }
}
