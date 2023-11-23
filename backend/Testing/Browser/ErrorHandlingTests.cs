using LexBoxApi.Auth;
using Shouldly;
using Testing.Browser.Base;
using Testing.Browser.Page;
using Testing.Browser.Page.External;
using Testing.Services;

namespace Testing.Browser;

[Trait("Category", "Integration")]
public class ErrorHandlingTests : PageTest
{
    [Fact]
    public async Task CatchGoto500InSameTab()
    {
        await new SandboxPage(Page).Goto();
        await Page.RunAndWaitForResponseAsync(async () =>
        {
            await Page.GetByText("Goto API 500", new() { Exact = true }).ClickAsync();
        }, "/api/testing/test500NoException");
        ExpectDeferredException();
    }

    [Fact]
    public async Task CatchGoto500InNewTab()
    {
        await new SandboxPage(Page).Goto();
        await Context.RunAndWaitForPageAsync(async () =>
        {
            await Page.GetByText("Goto API 500 new tab").ClickAsync();
        });
        ExpectDeferredException();
    }

    [Fact]
    public async Task CatchPageLoad500()
    {
        await new SandboxPage(Page).Goto();
        await Page.GetByText("Goto page load 500", new() { Exact = true }).ClickAsync();
        ExpectDeferredException();
        await Expect(Page.Locator(":text-matches('Unexpected response:.*(500)', 'g')").First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task PageLoad500InNewTabLandsOnErrorPage()
    {
        await new SandboxPage(Page).Goto();
        var newPage = await Context.RunAndWaitForPageAsync(async () =>
        {
            await Page.GetByText("Goto page load 500 new tab").ClickAsync();
        });
        await newPage.WaitForResponseAsync("**");
        ExpectDeferredException();
        await Expect(newPage.Locator(":text-matches('Unexpected response:.*(500)', 'g')").First).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CatchFetch500AndErrorDialog()
    {
        await new SandboxPage(Page).Goto();
        await Page.RunAndWaitForResponseAsync(async () =>
        {
            await Page.GetByText("Fetch 500").ClickAsync();
        }, "/api/testing/test500NoException");
        ExpectDeferredException();
        await Expect(Page.Locator(".modal-box.bg-error:text-matches('Unexpected response:.*(500)', 'g')")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task ServerPageLoad403IsRedirectedToLogin()
    {
        await SetCookies(new[] { $"{AuthKernel.AuthCookieName}={TestConstants.InvalidJwt}" });
        await new UserDashboardPage(Page).Goto(new() { ExpectRedirect = true });
        await new LoginPage(Page).WaitFor();
    }

    [Fact]
    public async Task ClientPageLoad403IsRedirectedToLogin()
    {
        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);
        var adminDashboardPage = await new AdminDashboardPage(Page).Goto();

        await SetCookies(new[] { $"{AuthKernel.AuthCookieName}={TestConstants.InvalidJwt}" });

        var response = await Page.RunAndWaitForResponseAsync(async () =>
        {
            await adminDashboardPage.ClickProject("Sena 3");
        }, "/api/graphql");

        response.Status.ShouldBe(401);
        await new LoginPage(Page).WaitFor();
    }

    [Fact]
    public async Task CatchGoto403InSameTab()
    {

        await new SandboxPage(Page).Goto();
        await Page.RunAndWaitForResponseAsync(async () =>
        {
            await Page.GetByText("Goto API 403", new() { Exact = true }).ClickAsync();
        }, "/api/AuthTesting/403");
        ExpectDeferredException();
    }

    [Fact]
    public async Task CatchGoto403InNewTab()
    {
        await new SandboxPage(Page).Goto();
        await Context.RunAndWaitForPageAsync(async () =>
        {
            await Page.GetByText("Goto API 403 new tab").ClickAsync();
        });
        ExpectDeferredException();
    }

    [Fact]
    public async Task PageLoad403IsRedirectedToHome()
    {
        await LoginAs("manager", TestingEnvironmentVariables.DefaultPassword);
        await new SandboxPage(Page).Goto();
        await Page.GetByText("Goto page load 403", new() { Exact = true }).ClickAsync();
        await new UserDashboardPage(Page).WaitFor();
    }

    [Fact]
    public async Task PageLoad403InNewTabIsRedirectedToHome()
    {
        await LoginAs("manager", TestingEnvironmentVariables.DefaultPassword);
        await new SandboxPage(Page).Goto();
        var newPage = await Context.RunAndWaitForPageAsync(async () =>
        {
            await Page.GetByText("Goto page load 403 new tab").ClickAsync();
        });
        await new UserDashboardPage(newPage).WaitFor();
    }

    [Fact]
    public async Task PageLoad403OnHomePageIsRedirectedToLogin()
    {
        // (1) Get JWT with only forgot-password audience
        // - Register
        var mailinatorId = Guid.NewGuid().ToString();
        var email = $"{mailinatorId}@mailinator.com";
        var password = email;
        await using var userDashboardPage = await RegisterUser($"Test: {nameof(PageLoad403OnHomePageIsRedirectedToLogin)} - {mailinatorId}", email, password);

        // - Request forgot password email
        var loginPage = await Logout();
        var forgotPasswordPage = await loginPage.ClickForgotPassword();
        await forgotPasswordPage.FillForm(email);
        await forgotPasswordPage.Submit();

        // - Get JWT from reset password link
        var inboxPage = await MailInboxPage.Get(Page, mailinatorId).Goto();
        var emailPage = await inboxPage.OpenEmail();
        var url = await emailPage.GetFirstLanguageDepotUrl();
        url.ShouldNotBeNull().ShouldNotBeEmpty();
        var forgotPasswordJwt = url.Split("jwt=")[1].Split("&")[0];

        // (2) Get to a non-home page with an empty urql cache
        await LoginAs(email, password);
        var userAccountPage = await new UserAccountSettingsPage(Page).Goto();

        // (3) Update cookie with the reset-password audience JWT and try to go home
        await SetCookies(new[] { $"{AuthKernel.AuthCookieName}={forgotPasswordJwt}" });

        var response = await Page.RunAndWaitForResponseAsync(userAccountPage.GoHome, "/api/graphql");
        response.Status.ShouldBe(403);

        // (4) Expect to be redirected to login page
        await new LoginPage(Page).WaitFor();
    }

    [Fact]
    public async Task NodeSurvivesCorruptJwt()
    {
        var corruptJwt = "bla-bla-bla";
        await SetCookies(new[] { $"{AuthKernel.AuthCookieName}={corruptJwt}" });
        await new UserDashboardPage(Page).Goto(new() { ExpectRedirect = true });
        await new LoginPage(Page).WaitFor();
    }
}
