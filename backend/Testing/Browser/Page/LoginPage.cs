using Microsoft.Playwright;
using Testing.Browser.Util;

namespace Testing.Browser.Page;

public class LoginPage : BasePage<LoginPage>
{
    public LoginPage(IPage page)
    : base(page, "/login", page.GetByRole(AriaRole.Heading, new() { Name = "Log in" }))
    {
    }

    public async Task FillForm(string emailOrUsername, string password)
    {
        await Page.GetByLabel("Email").FillAsync(emailOrUsername);
        await Page.GetByLabel("Password").FillAsync(password);
    }

    public async Task Submit()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();
    }

    public Task<ForgotPasswordPage> ClickForgotPassword()
    {
        return TaskUtil.WhenAllTakeSecond(
            Page.GetByRole(AriaRole.Link, new() { Name = "Forgot password" }).ClickAsync(),
            new ForgotPasswordPage(Page).WaitFor());
    }
}
