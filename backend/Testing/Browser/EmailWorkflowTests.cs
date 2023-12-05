using Testing.Browser.Base;
using Testing.Browser.Page;
using Testing.Browser.Page.External;

namespace Testing.Browser;

[Trait("Category", "Integration")]
public class EmailWorkflowTests : PageTest
{
    [Fact]
    public async Task RegisterVerifyUpdateVerifyEmailAdress()
    {
        // Step: Register
        var mailinatorId = Guid.NewGuid().ToString();
        var email = $"{mailinatorId}@mailinator.com";
        var password = email;
        await using var userDashboardPage = await RegisterUser($"Test: Verify email - {mailinatorId}", email, password);

        await userDashboardPage.EmailVerificationAlert.AssertPleaseVerify();

        // Step: Request extra verification email and verify
        await userDashboardPage.EmailVerificationAlert.ClickResendEmail();
        await userDashboardPage.EmailVerificationAlert.AssertVerificationSent();

        // await Page.PauseAsync(); // Pause in dev to forward e-mails to mailinator

        var inboxPage = await MailInboxPage.Get(Page, mailinatorId).Goto();
        await Expect(inboxPage.EmailLocator).ToHaveCountAsync(2);
        var emailPage = await inboxPage.OpenEmail();
        var newPage = await Page.Context.RunAndWaitForPageAsync(emailPage.ClickVerifyEmail);
        var userPage = await new UserAccountSettingsPage(newPage).WaitFor();

        await userPage.EmailVerificationAlert.AssertSuccessfullyVerified();

        // Step: Verify verification alert goes away on navigation
        await userPage.GoHome();
        await userPage.EmailVerificationAlert.AssertGone();

        // Step: Request new e-mail address
        await userPage.Goto();
        var newMailinatorId = Guid.NewGuid().ToString();
        var newEmail = $"{newMailinatorId}@mailinator.com";
        await userPage.FillEmail(newEmail);
        await userPage.ClickSave();

        await userPage.EmailVerificationAlert.AssertRequestedToChange();

        // await Page.PauseAsync(); // Pause in dev to forward e-mail to mailinator

        // Step: Verify new e-mail address
        await inboxPage.GotoMailbox(newMailinatorId);
        await Expect(inboxPage.EmailLocator).ToHaveCountAsync(1);
        emailPage = await inboxPage.OpenEmail();
        newPage = await Page.Context.RunAndWaitForPageAsync(emailPage.ClickVerifyEmail);
        userPage = await new UserAccountSettingsPage(newPage).WaitFor();

        await userPage.EmailVerificationAlert.AssertSuccessfullyUpdated();

        // Step: Confirm new e-mail address works
        var loginPage = await Logout();
        await loginPage.FillForm(newEmail, password);
        await loginPage.Submit();
        await userDashboardPage.WaitFor();
    }

    [Fact]
    public async Task ForgotPassword()
    {
        // Step: Register
        var mailinatorId = Guid.NewGuid().ToString();
        var email = $"{mailinatorId}@mailinator.com";
        var password = email;
        await using var userDashboardPage = await RegisterUser($"Test: Forgot password - {mailinatorId}", email, password);

        // Step: Request forgot password email
        var loginPage = await Logout();
        var forgotPasswordPage = await loginPage.ClickForgotPassword();
        await forgotPasswordPage.FillForm(email);
        await forgotPasswordPage.Submit();

        // Step: Use reset password link
        var inboxPage = await MailInboxPage.Get(Page, mailinatorId).Goto();
        var emailPage = await inboxPage.OpenEmail();

        var newPage = await Page.Context.RunAndWaitForPageAsync(emailPage.ClickResetPassword);
        var resetPasswordPage = await new ResetPasswordPage(newPage).WaitFor();

        var newPassword = Guid.NewGuid().ToString();
        await resetPasswordPage.FillForm(newPassword);
        await resetPasswordPage.Submit();

        // Step: Confirm new password works
        loginPage = await Logout();
        await loginPage.FillForm(email, newPassword);
        await loginPage.Submit();
        await userDashboardPage.WaitFor();

        // Step: Verify email link has expired
        inboxPage = await MailInboxPage.Get(Page, mailinatorId).Goto();
        emailPage = await inboxPage.OpenEmail(1); // 0 is the password changed notification
        newPage = await Page.Context.RunAndWaitForPageAsync(emailPage.ClickResetPassword);
        loginPage = await new LoginPage(newPage).WaitFor();
        await Expect(loginPage.Page.GetByText("The email you clicked has expired")).ToBeVisibleAsync();
    }
}
