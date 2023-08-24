using Testing.Browser.Base;
using Testing.Browser.Page;
using Testing.Browser.Page.External;
using Testing.Browser.Util;

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

        MailInboxPage inboxPage = IsDev
            ? await new MailDevInboxPage(Page, mailinatorId).Goto()
            : await new MailinatorInboxPage(Page, mailinatorId).Goto();
        await Expect(inboxPage.EmailLocator).ToHaveCountAsync(2);
        var emailPage = await inboxPage.OpenEmail();
        var userPage = await TaskUtil.WhenAllTakeSecond(
            emailPage.ClickVerifyEmail(),
            new UserAccountSettingsPage(await Page.Context.WaitForPageAsync()).WaitFor()
        );

        await userPage.EmailVerificationAlert.AssertSuccessfullyVerified();

        // Step: Verify verification alert is gone
        await userPage.Page.ReloadAsync(); // So we don't have to wait ~10 seconds for the alert to disappear
        await userPage.WaitFor();
        await userPage.EmailVerificationAlert.AssertGone();

        // Step: Request new e-mail address
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
        userPage = await TaskUtil.WhenAllTakeSecond(
            emailPage.ClickVerifyEmail(),
            new UserAccountSettingsPage(await Page.Context.WaitForPageAsync()).WaitFor()
        );

        await userPage.EmailVerificationAlert.AssertSuccessfullyUpdated();

        // Step: Confirm new e-mail address works
        var loginPage = await Logout();
        await loginPage.FillForm(newEmail, password);
        await Task.WhenAll(
            loginPage.Submit(),
            userDashboardPage.WaitFor());
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
        MailInboxPage inboxPage = IsDev
            ? await new MailDevInboxPage(Page, mailinatorId).Goto()
            : await new MailinatorInboxPage(Page, mailinatorId).Goto();
        var emailPage = await inboxPage.OpenEmail();

        var resetPasswordPage = await TaskUtil.WhenAllTakeSecond(
            emailPage.ClickResetPassword(),
            new ResetPasswordPage(await Page.Context.WaitForPageAsync()).WaitFor());

        var newPassword = Guid.NewGuid().ToString();
        await resetPasswordPage.FillForm(newPassword);
        await resetPasswordPage.Submit();

        // Step: Confirm new password works
        loginPage = await Logout();
        await loginPage.FillForm(email, newPassword);
        await Task.WhenAll(
            loginPage.Submit(),
            userDashboardPage.WaitFor());
    }
}
