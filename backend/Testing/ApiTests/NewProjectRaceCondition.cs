using System.Text.Json.Nodes;
using Shouldly;
using Testing.Services;

namespace Testing.ApiTests;

// Issue: https://github.com/sillsdev/languageforge-lexbox/issues/173#issuecomment-1665478630
[Trait("Category", "Integration")]
public class NewProjectRaceCondition : ApiTestBase
{
    [Fact]
    public async Task CanCreateMultipleProjectsAndQueryThemRightAway()
    {
        var project1Id = Guid.Parse("3e81814d-ce7e-438f-1111-beac1cd7596b");
        var project2Id = Guid.Parse("3e81814d-ce7e-438f-2222-beac1cd7596b");

        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);

        try
        {
            await CreateQueryAndVerifyProject(project1Id);
            // Create and query a 2nd project to ensure the HgWeb refresh-interval doesn't cause trouble
            await CreateQueryAndVerifyProject(project2Id);
        }
        finally
        {
            await HttpClient.DeleteAsync($"{BaseUrl}/api/project/{project1Id}");
            await HttpClient.DeleteAsync($"{BaseUrl}/api/project/{project2Id}");
        }
    }

    private async Task CreateQueryAndVerifyProject(Guid id)
    {
        var name = $"Name:{id}";

        var response = await ExecuteGql($$"""
            mutation {
                createProject(input: {
                    name: "{{name}}",
                    type: FL_EX,
                    id: "{{id}}",
                    code: "{{id}}",
                    description: "this is just a testing project for testing a race condition",
                    retentionPolicy: DEV
                }) {
                    createProjectResponse {
                        id
                    }
                }
            }
            """);

        var project = response["data"]!["createProject"]!["createProjectResponse"].ShouldBeOfType<JsonObject>();
        project["id"]!.GetValue<string>().ShouldBe(id.ToString());

        // Query a 2nd time to ensure the instability of new repos isn't causing trouble
        response = await ExecuteGql($$"""
            query {
                projectByCode(code: "{{id}}") {
                    name
                    changesets {
                        desc
                    }
                }
            }
            """);

        project = response["data"]!["projectByCode"].ShouldBeOfType<JsonObject>();
        project["name"]!.GetValue<string>().ShouldBe(name);
    }
}
