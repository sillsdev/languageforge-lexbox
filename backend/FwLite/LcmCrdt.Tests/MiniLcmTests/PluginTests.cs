using FluentValidation;
using MiniLcm.Tests;

namespace LcmCrdt.Tests.MiniLcmTests;

public class PluginTests(MiniLcmApiFixture fixture) : IClassFixture<MiniLcmApiFixture>
{
    private const string ManagerUserId = "manager-user";
    private const string EditorUserId = "editor-user";

    private IMiniLcmApi? _api;
    // The full production wrapper stack, not the raw fixture.Api — plugin writes must be
    // explicitly forwarded through the normalization/validation wrappers, and only a wrapped
    // API exercises that.
    private IMiniLcmApi Api => _api ??= TestMiniLcmWrappers.CreateUserFacingWrappers().Apply(fixture.Api, null!);

    private async Task SetCurrentUser(string userId, UserProjectRole role)
    {
        var projectService = fixture.GetService<CurrentProjectService>();
        await projectService.UpdateLastUser("test-user", userId);
        await projectService.UpdateUserRole(role);
    }

    private Plugin NewPlugin(string name)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Html = "<html><body><h1>Test plugin</h1></body></html>",
        };
    }

    private async Task<Plugin> CreatePluginAsManager(string name = "Owned")
    {
        await SetCurrentUser(ManagerUserId, UserProjectRole.Manager);
        return await Api.CreatePlugin(NewPlugin(name));
    }

    [Fact]
    public async Task CreatePlugin_AllowsManager()
    {
        await SetCurrentUser(ManagerUserId, UserProjectRole.Manager);

        var created = await Api.CreatePlugin(NewPlugin("My Plugin"));

        var allPlugins = await Api.GetPlugins().ToArrayAsync();
        allPlugins.Should().Contain(p => p.Id == created.Id && p.Name == "My Plugin");
    }

    [Fact]
    public async Task CreatePlugin_RejectsEditor()
    {
        await SetCurrentUser(EditorUserId, UserProjectRole.Editor);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Api.CreatePlugin(NewPlugin("Should Fail")));
    }

    [Fact]
    public async Task CreatePlugin_RejectsEmptyName()
    {
        await SetCurrentUser(ManagerUserId, UserProjectRole.Manager);
        var plugin = NewPlugin("  ");
        await Assert.ThrowsAsync<ValidationException>(() => Api.CreatePlugin(plugin));
    }

    [Fact]
    public async Task CreatePlugin_RejectsEmptyHtml()
    {
        await SetCurrentUser(ManagerUserId, UserProjectRole.Manager);
        var plugin = NewPlugin("My Plugin") with { Html = "" };
        await Assert.ThrowsAsync<ValidationException>(() => Api.CreatePlugin(plugin));
    }

    [Fact]
    public async Task UpdatePlugin_AllowsManager()
    {
        var created = await CreatePluginAsManager("My Plugin");
        await Api.UpdatePlugin(created with { Name = "My Updated Plugin", Html = "<html><body>v2</body></html>" });

        var fetched = await Api.GetPlugin(created.Id);
        fetched.Should().NotBeNull();
        fetched.Name.Should().Be("My Updated Plugin");
        fetched.Html.Should().Be("<html><body>v2</body></html>");
    }

    [Fact]
    public async Task Plugin_PersistsAndClearsOptionalDescription()
    {
        await SetCurrentUser(ManagerUserId, UserProjectRole.Manager);
        var created = await Api.CreatePlugin(NewPlugin("Described") with { Description = "First summary" });
        (await Api.GetPlugin(created.Id))!.Description.Should().Be("First summary");

        await Api.UpdatePlugin(created with { Description = "Updated summary" });
        (await Api.GetPlugin(created.Id))!.Description.Should().Be("Updated summary");

        // An edit that omits the description clears it (the full plugin is written each time).
        await Api.UpdatePlugin(created with { Description = null });
        (await Api.GetPlugin(created.Id))!.Description.Should().BeNull();
    }

    [Fact]
    public async Task UpdatePlugin_RejectsEditor()
    {
        var created = await CreatePluginAsManager();

        await SetCurrentUser(EditorUserId, UserProjectRole.Editor);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            Api.UpdatePlugin(created with { Name = "Should Fail" }));
    }

    [Fact]
    public async Task DeletePlugin_AllowsManager()
    {
        var plugin = await CreatePluginAsManager();

        await SetCurrentUser(ManagerUserId, UserProjectRole.Manager);
        await Api.DeletePlugin(plugin.Id);

        var deleted = await Api.GetPlugin(plugin.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeletePlugin_RejectsEditor()
    {
        var plugin = await CreatePluginAsManager();

        await SetCurrentUser(EditorUserId, UserProjectRole.Editor);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Api.DeletePlugin(plugin.Id));
    }

    [Fact]
    public async Task GetPlugins_ReturnsAllPlugins_ForEditor()
    {
        var plugin1 = await CreatePluginAsManager("Plugin 1");
        var plugin2 = await CreatePluginAsManager("Plugin 2");

        await SetCurrentUser(EditorUserId, UserProjectRole.Editor);
        var visible = await Api.GetPlugins().ToArrayAsync();

        visible.Should().Contain(p => p.Id == plugin1.Id);
        visible.Should().Contain(p => p.Id == plugin2.Id);
    }
}
