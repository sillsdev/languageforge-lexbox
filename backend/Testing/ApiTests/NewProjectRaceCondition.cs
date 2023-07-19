using System.Text.Json.Nodes;
using Shouldly;

namespace Testing.ApiTests;

// issue: https://github.com/sillsdev/languageforge-lexbox/issues/173
[Trait("Category", "Integration")]
public class NewProjectRaceCondition : ApiTestBase
{
    readonly Guid _projectId = Guid.Parse("3e81814d-ce7e-438f-b8e8-beac1cd7596b");
    [Fact]
    public async Task CanCreateProjectAndQueryItRightAway()
    {

        await LoginAs("admin", "pass");
        await HttpClient.DeleteAsync($"http://{Host}/api/project/project/{_projectId}");
        await Task.Delay(TimeSpan.FromSeconds(1));
        var response = await ExecuteGql($$"""
mutation {
    createProject(input: {
        name: "Test race condition",
        type: FL_EX,
        id: "{{_projectId}}",
        code: "test-race-flex",
        description: "this is just a testing project for testing a race condition",
        retentionPolicy: DEV
    }) {
        project {
            name
            changesets {
                desc
            }
        }
    }
}
""");
        var project = response["data"]!["createProject"]!["project"].ShouldBeOfType<JsonObject>();
        project["name"]!.GetValue<string>().ShouldBe("test");
    }
}
