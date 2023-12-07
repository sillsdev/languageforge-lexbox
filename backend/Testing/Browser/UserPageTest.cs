using Testing.Browser.Base;
using Testing.Browser.Page;
using Testing.Browser.Page.External;

namespace Testing.Browser;

[Trait("Category", "Integration")]
public class UserPageTest : PageTest
{
    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();
    }

    [Fact]
    public async Task CanUpdateAccountInfo()
    {
        await using var userDashboardPage = await RegisterUser("Test: Edit account - update", $"{Guid.NewGuid()}@mailinator.com", "test_edit_account_update");
        var userPage = await new UserAccountSettingsPage(Page).Goto();
        await userPage.FillDisplayName("Test: Edit account - update - changed");
        await userPage.FillEmail($"{Guid.NewGuid()}_changed@test.com");
        await userPage.ClickSave();
        await Page.GetByText("Your account has been updated.").WaitForAsync();
        await userPage.EmailVerificationAlert.AssertRequestedToChange();
    }

    [Fact]
    public async Task DisplaysFormErrorsOnInvalidData()
    {
        await using var userDashboardPage = await RegisterUser("Test: Edit account - errors", $"{Guid.NewGuid()}@mailinator.com", "test_edit_account_errors");
        var userPage = await new UserAccountSettingsPage(Page).Goto();
        await userPage.FillDisplayName("");
        await userPage.FillEmail("");
        await userPage.ClickSave();
        await Expect(Page.GetByText("Invalid email")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CanResetPassword()
    {
        var mailinatorId = Guid.NewGuid().ToString();
        await using var userDashboardPage = await RegisterUser("Test: Edit account - reset password", $"{mailinatorId}@mailinator.com", "test_edit_account_reset_password");
        var user = userDashboardPage.User;
        var userPage = await new UserAccountSettingsPage(Page).Goto();
        var resetPasswordPage = await userPage.ClickResetPassword();
        var newPassword = "test_edit_account_reset_password_changed";
        await resetPasswordPage.FillForm(newPassword);
        await resetPasswordPage.Submit();

        var loginPage = await Logout();
        await loginPage.FillForm(userDashboardPage.User.Email, newPassword);
        await loginPage.Submit();
        await new UserDashboardPage(Page).WaitFor();

        //verify password changed email was received
        var inboxPage = await MailInboxPage.Get(Page, mailinatorId).Goto();
        var emailPage = await inboxPage.OpenEmail();
        await Expect(emailPage.Page.GetByText("Your password was changed").First).ToBeVisibleAsync();
    }
}
