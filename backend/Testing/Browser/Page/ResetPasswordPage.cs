using Microsoft.Playwright;

namespace Testing.Browser.Page;

public class ResetPasswordPage : AuthenticatedBasePage<ResetPasswordPage>
{
    public ResetPasswordPage(IPage page)
    : base(page, "/resetPassword", page.GetByRole(AriaRole.Button, new() { Name = "Reset Password" }))
    {
    }

    public async Task FillForm(string newPassword)
    {
        await Page.GetByLabel("New Password").FillAsync(newPassword);
    }

    public async Task<UserDashboardPage> Submit()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Reset Password" }).ClickAsync();
        return await new UserDashboardPage(Page).WaitFor();
    }
}
