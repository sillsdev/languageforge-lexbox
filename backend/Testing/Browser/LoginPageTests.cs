﻿using Microsoft.Playwright;
using Testing.Browser.Base;
using Testing.Services;

namespace Testing.Browser;

[Trait("Category", "Integration")]
public class LoginPageTests: PageTest
{
    [Fact]
    public async Task CanLoginClicking()
    {
        await Page.GotoAsync($"https://{TestingEnvironmentVariables.ServerHostname}/login");

        await Page.GetByLabel("Email (or Send/Receive username)").ClickAsync();

        await Page.GetByLabel("Email (or Send/Receive username)").FillAsync("KindLion");

        await Page.GetByLabel("Password").ClickAsync();

        await Page.GetByLabel("Password").FillAsync("pass");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync($"https://{TestingEnvironmentVariables.ServerHostname}/admin");
    }

    [Fact]
    public async Task CanLoginKeyboardNavigation()
    {
        await Page.GotoAsync($"https://{TestingEnvironmentVariables.ServerHostname}/login");

        await Page.GetByLabel("Email (or Send/Receive username)").FillAsync("KindLion");

        await Page.GetByLabel("Email (or Send/Receive username)").PressAsync("Tab");

        await Page.GetByLabel("Password").FillAsync("pass");

        await Page.GetByLabel("Password").PressAsync("Enter");

        await Expect(Page).ToHaveURLAsync($"https://{TestingEnvironmentVariables.ServerHostname}/admin");
    }

    [Fact]
    public async Task ShowErrorWithoutUsername()
    {
        await Page.GotoAsync($"https://{TestingEnvironmentVariables.ServerHostname}/login");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync($"https://{TestingEnvironmentVariables.ServerHostname}/login");
        await Expect(Page.GetByText("User info missing")).ToBeVisibleAsync();
    }
}
