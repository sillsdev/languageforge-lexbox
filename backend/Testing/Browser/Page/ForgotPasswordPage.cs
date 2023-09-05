using Microsoft.Playwright;

namespace Testing.Browser.Page;

public class ForgotPasswordPage : BasePage<ForgotPasswordPage>
{
    public ForgotPasswordPage(IPage page)
    : base(page, "/forgotPassword", page.GetByRole(AriaRole.Heading, new() { Name = "Forgot Password" }))
    {
    }

    public async Task FillForm(string email)
    {
        await Page.GetByLabel("Email").FillAsync(email);
    }

    public async Task<ResetPasswordEmailSentPage> Submit()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Send reset email" }).ClickAsync();
        return await new ResetPasswordEmailSentPage(Page).WaitFor();
    }
}

public class ResetPasswordEmailSentPage : BasePage<ResetPasswordEmailSentPage>
{
    public ResetPasswordEmailSentPage(IPage page)
    : base(page, "/forgotPassword/emailSent", page.GetByRole(AriaRole.Heading, new() { Name = "Check Your Inbox" }))
    {
    }
}
