using Microsoft.Playwright;
using Testing.Browser.Util;

namespace Testing.Browser.Page;

public class UserAccountSettingsPage : AuthenticatedBasePage<UserAccountSettingsPage>
{
    public UserAccountSettingsPage(IPage page)
    : base(page, "/user", page.GetByRole(AriaRole.Heading, new() { Name = "Account Settings" }))
    {
    }

    internal Task FillDisplayName(string name)
    {
        return Page.GetByLabel("Display name").FillAsync(name);
    }

    internal Task FillEmail(string email)
    {
        return Page.GetByLabel("Email").FillAsync(email);
    }

    internal async Task ClickSave()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Update account" }).ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }

    internal Task<ResetPasswordPage> ClickResetPassword()
    {
        return TaskUtil.WhenAllTakeSecond(
            Page.GetByRole(AriaRole.Link, new() { Name = "Reset your password" }).ClickAsync(),
            new ResetPasswordPage(Page).WaitFor());
    }
}
