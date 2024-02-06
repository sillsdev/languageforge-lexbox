using Microsoft.Playwright;

namespace Testing.Browser.Page;

public class AdminDashboardPage : AuthenticatedBasePage<AdminDashboardPage>
{
    private ILocator ProjectFilterBarInput => Page.Locator(".filter-bar").Nth(0).GetByRole(AriaRole.Textbox);

    public AdminDashboardPage(IPage page)
    : base(page, "/admin", page.Locator(".breadcrumbs :text('Admin Dashboard')"))
    {
    }

    public async Task<ProjectPage> OpenProject(string projectName, string projectCode)
    {
        await ClickProject(projectName);
        return await new ProjectPage(Page, projectName, projectCode).WaitFor();
    }

    public async Task ClickProject(string projectName)
    {
        await ProjectFilterBarInput.FillAsync(projectName); // make sure the project is visible
        var projectTable = Page.Locator("table").Nth(0);
        await projectTable.GetByRole(AriaRole.Link, new() { Name = projectName, Exact = true }).ClickAsync();
    }
}
