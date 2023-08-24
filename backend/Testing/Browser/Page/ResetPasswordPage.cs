using Microsoft.Playwright;
using Testing.Browser.Util;

namespace Testing.Browser.Page;

public class ResetPasswordPage : AuthenticatedBasePage<ResetPasswordPage>
{
    public ResetPasswordPage(IPage page)
    : base(page, "/resetPassword", page.GetByRole(AriaRole.Heading, new() { Name = "Reset Password" }))
    {
    }

    public async Task FillForm(string newPassword)
    {
        await Page.GetByLabel("New Password").FillAsync(newPassword);
    }

    public Task<UserDashboardPage> Submit()
    {
        return TaskUtil.WhenAllTakeSecond(
            Page.GetByRole(AriaRole.Button, new() { Name = "Reset Password" }).ClickAsync(),
            new UserDashboardPage(Page).WaitFor());
    }
}
