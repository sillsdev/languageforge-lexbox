using Microsoft.Playwright;

namespace Testing.Browser.Component;

public class EmailVerificationAlert : BaseComponent<EmailVerificationAlert>
{
    private const string PLEASE_VERIFY_SELECTOR = ":text('We sent you an email, so that you can verify your email address')";
    private const string VERIFICATION_SENT_SELECTOR = ":text('check your inbox for the verification email')";
    private const string SUCCESSFULLY_VERIFIED_SELECTOR = ":text('successfully verified')";
    private const string REQUESTED_TO_CHANGE_SELECTOR = ":text('requested to change')";
    private const string SUCCESSFULLY_UPDATED_SELECTOR = ":text('successfully updated')";
    private static readonly string CONTENT_SELECTOR = string.Join(", ", new[] {
        PLEASE_VERIFY_SELECTOR,
        VERIFICATION_SENT_SELECTOR,
        SUCCESSFULLY_VERIFIED_SELECTOR,
        REQUESTED_TO_CHANGE_SELECTOR,
        SUCCESSFULLY_UPDATED_SELECTOR });

    public EmailVerificationAlert(IPage page) : base(page, page.Locator(".alert", new() { Has = page.Locator(CONTENT_SELECTOR) }))
    {
    }

    public Task AssertPleaseVerify()
    {
        return Assertions.Expect(Locator(PLEASE_VERIFY_SELECTOR)).ToBeVisibleAsync();
    }

    public Task AssertVerificationSent()
    {
        return Assertions.Expect(Locator(VERIFICATION_SENT_SELECTOR)).ToBeVisibleAsync();
    }

    public Task AssertSuccessfullyVerified()
    {
        return Assertions.Expect(Locator(SUCCESSFULLY_VERIFIED_SELECTOR)).ToBeVisibleAsync();
    }

    public Task AssertRequestedToChange()
    {
        return Assertions.Expect(Locator(REQUESTED_TO_CHANGE_SELECTOR)).ToBeVisibleAsync();
    }

    public Task AssertSuccessfullyUpdated()
    {
        return Assertions.Expect(Locator(SUCCESSFULLY_UPDATED_SELECTOR)).ToBeVisibleAsync();
    }

    public async Task ClickResendEmail()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Resend verification email" }).ClickAsync();
    }
}
