using Microsoft.Playwright;

namespace Testing.Browser.Page;

public class SandboxPage : BasePage<SandboxPage>
{
    public SandboxPage(IPage page) : base(page, "/sandbox", page.GetByRole(AriaRole.Heading, new() { Name = "Sandbox" }))
    {
    }
}
