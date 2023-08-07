using Microsoft.Playwright;

namespace Testing.Browser.Page;

public class UserAccountSettingsPage : AuthenticatedBasePage<UserAccountSettingsPage>
{
    public UserAccountSettingsPage(IPage page)
    : base(page, "/user", page.GetByRole(AriaRole.Heading, new() { Name = "Account Settings" }))
    {
    }

    internal Task FillEmail(string email)
    {
        return Page.GetByLabel("Email").FillAsync(email);
    }

    internal Task ClickSave()
    {
        return Page.GetByRole(AriaRole.Button, new() { Name = "Update account" }).ClickAsync();
    }
}
