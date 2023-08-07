using Microsoft.Playwright;

namespace Testing.Browser.Page;

public class ProjectPage : AuthenticatedBasePage<ProjectPage>
{
    public ProjectPage(IPage page, string name, string code)
    : base(page, $"/project/{code}", page.Locator($".breadcrumbs :text('{name}')"))
    {
    }
}
