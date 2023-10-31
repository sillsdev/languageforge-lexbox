using System.Net;
using System.Net.Http.Json;
using LexCore.Auth;
using LexCore.Utils;
using Microsoft.Playwright;
using Shouldly;
using Testing.Browser.Page;
using Testing.Browser.Util;
using Testing.Services;

namespace Testing.Browser.Base;

public class PageTest : IAsyncLifetime
{
    protected bool EnableTrace { get; init; } = true;
    private readonly PlaywrightFixture _fixture;
    public IPage Page => _fixture.Page;
    public IBrowser Browser => _fixture.Browser;
    public IBrowserContext Context => _fixture.Context;
    /// <summary>
    /// Exceptions that are deferred until the end of the test, because they can't
    /// be cleanly thrown in sub-threads.
    /// </summary>
    private List<UnexpectedResponseException> DeferredExceptions { get; } = new();

    public PageTest()
    {
        _fixture = new PlaywrightFixture();
    }

    public ILocatorAssertions Expect(ILocator locator) => Assertions.Expect(locator);
    public IPageAssertions Expect(IPage page) => Assertions.Expect(page);
    public IAPIResponseAssertions Expect(IAPIResponse response) => Assertions.Expect(response);
    /// <summary>
    /// Consumes a deferred exception that was "thrown" in a sub-thread, and returns it
    /// or throws if no exception of the given type is found.
    /// </summary>
    public UnexpectedResponseException ExpectDeferredException()
    {
        var exception = DeferredExceptions.ShouldHaveSingleItem();
        DeferredExceptions.Clear();
        return exception;
    }

    public void ExpectNoDeferredExceptions()
    {
        DeferredExceptions.ShouldBeEmpty();
    }

    public virtual async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        if (EnableTrace)
        {
            await Context.Tracing.StartAsync(new()
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true
            });
        }

        Context.Response += (_, response) =>
        {
            if (response.Status >= (int)HttpStatusCode.InternalServerError)
            {
                DeferredExceptions.Add(new UnexpectedResponseException(response));
            }
        };
    }

    public virtual async Task DisposeAsync()
    {
        if (EnableTrace)
        {
            var now = FileUtils.ToTimestamp(DateTime.UtcNow);
            var testClass = GetType().Name;
            var tracePath = Path.Combine("playwright-traces", $"{testClass}_{now}.zip");
            await Context.Tracing.StopAsync(new() { Path = tracePath });
        }

        await _fixture.DisposeAsync();

        if (DeferredExceptions.Any())
        {
            throw new AggregateException(DeferredExceptions);
        }
    }

    static readonly HttpClient HttpClient = new HttpClient();

    public async Task LoginAs(string user, string password)
    {
        // TODO can't we just use Page.APIRequest instead
        var responseMessage = await HttpClient.PostAsJsonAsync(
            $"{TestingEnvironmentVariables.ServerBaseUrl}/api/login",
            new Dictionary<string, object>
            {
                { "password", password }, { "emailOrUsername", user }, { "preHashedPassword", false }
            });
        responseMessage.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase)
            .ShouldContainKey("Set-Cookie");
        var cookies = responseMessage.Headers.GetValues("Set-Cookie").ToArray();
        cookies.ShouldNotBeEmpty();
        var cookieContainer = new CookieContainer();
        foreach (var cookie in cookies)
        {
            cookieContainer.SetCookies(new($"{TestingEnvironmentVariables.ServerBaseUrl}"), cookie);
        }

        await Context.AddCookiesAsync(cookieContainer.GetAllCookies()
            .Select(cookie => new Microsoft.Playwright.Cookie
            {
                Value = cookie.Value,
                Domain = cookie.Domain,
                Expires = (float)cookie.Expires.Subtract(DateTime.UnixEpoch).TotalSeconds,
                Name = cookie.Name,
                Path = cookie.Path,
                Secure = cookie.Secure,
                HttpOnly = cookie.HttpOnly
            }));
    }

    protected async Task<LoginPage> Logout()
    {
        await Page.GotoAsync("/logout");
        return await new LoginPage(Page).WaitFor();
    }

    protected async Task<TempUserDashboardPage> RegisterUser(string name, string email, string password)
    {
        var registerPage = await new RegisterPage(Page).Goto();
        await registerPage.FillForm(name, email, password);
        await registerPage.Submit();
        await new UserDashboardPage(Page).WaitFor();
        var userId = await GetCurrentUserId();
        var user = new TempUser(userId, name, email, password);
        return new TempUserDashboardPage(Page, user);
    }

    private async Task<Guid> GetCurrentUserId()
    {
        var userResponse = await Page.APIRequest.GetAsync($"{TestingEnvironmentVariables.ServerBaseUrl}/api/user/currentUser");
        var user = await userResponse.JsonAsync<LexAuthUser>();
        return user.ShouldNotBeNull().Id;
    }
}

public class PlaywrightFixture : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        PlaywrightInstance = await Playwright.CreateAsync();
        Browser = await PlaywrightInstance.Chromium.LaunchAsync(new()
        {
            // Headless = false,
        }
        );
        Context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            BaseURL = $"{TestingEnvironmentVariables.ServerBaseUrl}/",
            StorageState = $$"""
                           {
                             "origins": [
                               {
                                 "origin": "{{TestingEnvironmentVariables.ServerHostname}}",
                                 "localStorage": [
                                   {
                                     "name": "isPlaywright",
                                     "value": "true"
                                   }
                                 ]
                               }
                             ]
                           }
                           """
        });
        Page = await Context.NewPageAsync();
    }

    public IPage Page { get; set; } = null!;
    public IBrowser Browser { get; set; } = null!;
    public IBrowserContext Context { get; set; } = null!;
    private IPlaywright PlaywrightInstance { get; set; } = null!;

    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        PlaywrightInstance.Dispose();
    }
}
