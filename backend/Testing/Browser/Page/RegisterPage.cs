using Microsoft.Playwright;

namespace Testing.Browser.Page;

public class RegisterPage : BasePage<RegisterPage>
{
    public RegisterPage(IPage page)
    : base(page, "/register", page.GetByRole(AriaRole.Heading, new() { Name = "Register" }))
    {
    }

    public async Task FillForm(string name, string email, string password)
    {
        await Page.GetByLabel("Name").FillAsync(name);
        await Page.GetByLabel("Email").FillAsync(email);
        await Page.GetByLabel("Password").FillAsync(password);
    }

    public async Task Submit()
    {
        await Page.GetByRole(AriaRole.Button, new() { Name = "Register" }).ClickAsync();
    }
}
