using LcmCrdt.Changes;

namespace LcmCrdt.Tests.MiniLcmTests;

public class CustomViewTests(MiniLcmApiFixture fixture) : IClassFixture<MiniLcmApiFixture>
{
    private async Task SetCurrentUser(string userId, UserProjectRole role = UserProjectRole.Editor)
    {
        var projectService = fixture.GetService<CurrentProjectService>();
        await projectService.UpdateLastUser("test-user", userId);
        await projectService.UpdateUserRole(role);
    }

    [Fact]
    public async Task CanCreateAndUpdateCustomView()
    {
        await SetCurrentUser("manager-user", UserProjectRole.Manager);

        var customView = new CustomView
        {
            Id = Guid.NewGuid(),
            Name = "My View",
            Base = ViewBase.FwLite,
            EntryFields = [new ViewField { FieldId = "lexemeForm" }],
            SenseFields = [new ViewField { FieldId = "gloss" }],
            ExampleFields = [new ViewField { FieldId = "sentence" }],
            Vernacular = [new WritingSystemId("en")],
            Analysis = [new WritingSystemId("en")],
        };

        var created = await fixture.Api.CreateCustomView(customView);
        created.Name.Should().Be("My View");

        var updated = await fixture.Api.UpdateCustomView(created.Id, created with { Name = "My Updated View" });

        updated.Name.Should().Be("My Updated View");
        var allCustomViews = await fixture.Api.GetCustomViews().ToArrayAsync();
        allCustomViews.Should().Contain(v => v.Id == created.Id && v.Name == "My Updated View");
    }

    [Fact]
    public async Task CreateCustomViewChange_CanBeApplied()
    {
        await SetCurrentUser("change-user");

        var customViewId = Guid.NewGuid();
        await fixture.DataModel.AddChange(
            Guid.NewGuid(),
            new CreateCustomViewChange(
                customViewId,
                new CustomView
                {
                    Id = customViewId,
                    Name = "Change View",
                    Base = ViewBase.FwLite,
                    EntryFields = [new ViewField { FieldId = "lexemeForm" }],
                    SenseFields = [new ViewField { FieldId = "gloss" }],
                    ExampleFields = [new ViewField { FieldId = "sentence" }],
                }));

        var customView = await fixture.Api.GetCustomView(customViewId);
        customView.Should().NotBeNull();
        customView!.Name.Should().Be("Change View");
        customView.EntryFields.Select(f => f.FieldId).Should().Equal(["lexemeForm"]);
        customView.SenseFields.Select(f => f.FieldId).Should().Equal(["gloss"]);
        customView.ExampleFields.Select(f => f.FieldId).Should().Equal(["sentence"]);
    }

    [Fact]
    public async Task GetCustomViews_ReturnsAllViews_ForEditor()
    {
        await SetCurrentUser("manager", UserProjectRole.Manager);
        var view1 = await fixture.Api.CreateCustomView(new CustomView
        {
            Id = Guid.NewGuid(),
            Name = "View 1",
            Base = ViewBase.FwLite,
            EntryFields = [new ViewField { FieldId = "lexemeForm" }],
            SenseFields = [],
            ExampleFields = [],
        });

        var view2 = await fixture.Api.CreateCustomView(new CustomView
        {
            Id = Guid.NewGuid(),
            Name = "View 2",
            Base = ViewBase.FwLite,
            EntryFields = [new ViewField { FieldId = "lexemeForm" }],
            SenseFields = [],
            ExampleFields = [],
        });

        await SetCurrentUser("editor", UserProjectRole.Editor);
        var visible = await fixture.Api.GetCustomViews().ToArrayAsync();
        visible.Should().Contain(v => v.Id == view1.Id);
        visible.Should().Contain(v => v.Id == view2.Id);
    }

    [Fact]
    public async Task CreateCustomView_RejectsEditor()
    {
        await SetCurrentUser("editor-user", UserProjectRole.Editor);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Api.CreateCustomView(new CustomView
        {
            Id = Guid.NewGuid(),
            Name = "Should Fail",
            Base = ViewBase.FwLite,
            EntryFields = [new ViewField { FieldId = "lexemeForm" }],
            SenseFields = [],
            ExampleFields = [],
        }));
    }

    [Fact]
    public async Task DeleteCustomView_RejectsEditor()
    {
        await SetCurrentUser("manager", UserProjectRole.Manager);
        var view = await fixture.Api.CreateCustomView(new CustomView
        {
            Id = Guid.NewGuid(),
            Name = "Owned",
            Base = ViewBase.FwLite,
            EntryFields = [new ViewField { FieldId = "lexemeForm" }],
            SenseFields = [],
            ExampleFields = [],
        });

        await SetCurrentUser("editor", UserProjectRole.Editor);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => fixture.Api.DeleteCustomView(view.Id));
    }

    [Fact]
    public async Task DeleteCustomView_AllowsManager()
    {
        await SetCurrentUser("manager", UserProjectRole.Manager);
        var view = await fixture.Api.CreateCustomView(new CustomView
        {
            Id = Guid.NewGuid(),
            Name = "Owned",
            Base = ViewBase.FwLite,
            EntryFields = [new ViewField { FieldId = "lexemeForm" }],
            SenseFields = [],
            ExampleFields = [],
        });

        await fixture.Api.DeleteCustomView(view.Id);
        var deleted = await fixture.Api.GetCustomView(view.Id);
        deleted.Should().BeNull();
    }
}
