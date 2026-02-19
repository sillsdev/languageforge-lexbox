namespace LcmCrdt.Tests.MiniLcmTests;

public class CustomViewTests(MiniLcmApiFixture fixture) : IClassFixture<MiniLcmApiFixture>
{
    private const string ManagerUserId = "manager-user";
    private const string EditorUserId = "editor-user";

    private async Task SetCurrentUser(string userId, UserProjectRole role)
    {
        var projectService = fixture.GetService<CurrentProjectService>();
        await projectService.UpdateLastUser("test-user", userId);
        await projectService.UpdateUserRole(role);
    }

    private CustomView NewCustomView(string name)
    {
        return new()
        {
            Id = Guid.NewGuid(),
            Name = name,
            Base = ViewBase.FwLite,
            EntryFields = [new ViewField { FieldId = "lexemeForm" }],
            SenseFields = [new ViewField { FieldId = "gloss" }],
            ExampleFields = [new ViewField { FieldId = "sentence" }],
            Vernacular = [new ViewWritingSystem { WsId = "en" }],
            Analysis = [new ViewWritingSystem { WsId = "en" }],
        };
    }


    private async Task<CustomView> CreateViewAsManager(string name = "Owned")
    {
        await SetCurrentUser(ManagerUserId, UserProjectRole.Manager);
        return await fixture.Api.CreateCustomView(NewCustomView(name));
    }

    [Fact]
    public async Task CreateCustomView_AllowsManager()
    {
        await SetCurrentUser(ManagerUserId, UserProjectRole.Manager);

        var created = await fixture.Api.CreateCustomView(NewCustomView("My View"));
        created.Name.Should().Be("My View");

        var allCustomViews = await fixture.Api.GetCustomViews().ToArrayAsync();
        allCustomViews.Should().Contain(v => v.Id == created.Id && v.Name == "My View");
    }

    [Fact]
    public async Task CreateCustomView_RejectsEditor()
    {
        await SetCurrentUser(EditorUserId, UserProjectRole.Editor);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Api.CreateCustomView(NewCustomView("Should Fail")));
    }

    [Fact]
    public async Task UpdateCustomView_AllowsManager()
    {
        var created = await CreateViewAsManager("My View");
        var updated = await fixture.Api.UpdateCustomView(created.Id, created with { Name = "My Updated View" });

        updated.Name.Should().Be("My Updated View");
        var allCustomViews = await fixture.Api.GetCustomViews().ToArrayAsync();
        allCustomViews.Should().Contain(v => v.Id == created.Id && v.Name == "My Updated View");
    }

    [Fact]
    public async Task UpdateCustomView_RejectsEditor()
    {
        var created = await CreateViewAsManager();

        await SetCurrentUser(EditorUserId, UserProjectRole.Editor);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            fixture.Api.UpdateCustomView(created.Id, created with { Name = "Should Fail" }));
    }

    [Fact]
    public async Task DeleteCustomView_AllowsManager()
    {
        var view = await CreateViewAsManager();

        await SetCurrentUser(ManagerUserId, UserProjectRole.Manager);
        await fixture.Api.DeleteCustomView(view.Id);

        var deleted = await fixture.Api.GetCustomView(view.Id);
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteCustomView_RejectsEditor()
    {
        var view = await CreateViewAsManager();

        await SetCurrentUser(EditorUserId, UserProjectRole.Editor);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Api.DeleteCustomView(view.Id));
    }

    [Fact]
    public async Task GetCustomViews_ReturnsAllViews_ForEditor()
    {
        var view1 = await CreateViewAsManager("View 1");
        var view2 = await CreateViewAsManager("View 2");

        await SetCurrentUser(EditorUserId, UserProjectRole.Editor);
        var visible = await fixture.Api.GetCustomViews().ToArrayAsync();

        visible.Should().Contain(v => v.Id == view1.Id);
        visible.Should().Contain(v => v.Id == view2.Id);
    }
}
