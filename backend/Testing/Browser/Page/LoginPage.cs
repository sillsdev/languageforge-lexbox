using Microsoft.Playwright;

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

    public async Task<ForgotPasswordPage> ClickForgotPassword()
    {
        await Page.GetByRole(AriaRole.Link, new() { Name = "Forgot your password?" }).ClickAsync();
        return await new ForgotPasswordPage(Page).WaitFor();
    }
}
