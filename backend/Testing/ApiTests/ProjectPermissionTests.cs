using System.Text.Json.Nodes;
using FluentAssertions;
using Testing.Services;

namespace Testing.ApiTests;

[Trait("Category", "Integration")]
public class ProjectPermissionTests : ApiTestBase
{
    private async Task<JsonObject> QueryProject(string projectCode, bool expectGqlError = false)
    {
        var json = await ExecuteGql(
            $$"""
              query {
                projectByCode(code: "{{projectCode}}") {
                  id
                  name
                  users {
                      user {
                          id
                          name
                      }
                  }
                }
              }
              """,
            expectGqlError);
        return json;
    }

    private async Task AddUserToProject(Guid projectId, string username)
    {
        await ExecuteGql(
            $$"""
              mutation {
                  addProjectMember(input: {
                      projectId: "{{projectId}}",
                      usernameOrEmail: "{{username}}",
                      role: EDITOR,
                      canInvite: false
                  }) {
                      project {
                          id
                      }
                      errors {
                          __typename
                          ... on Error {
                              message
                          }
                      }
                  }
              }
              """);
    }

    private JsonObject GetProject(JsonObject json)
    {
        var project = json["data"]!["projectByCode"]?.AsObject();
        project.Should().NotBeNull();
        return project;
    }

    private void MustHaveMembers(JsonObject project, int? count = null)
    {
        var members = project["users"]!.AsArray();
        members.Should().NotBeNullOrEmpty();
        if (count is not null) members.Count.Should().Be(count.Value);
    }

    private void MustNotHaveMembers(JsonObject project)
    {
        var users = project["users"]!.AsArray();
        users.Should().BeEmpty();
    }

    private void MustHaveOnlyUserAsMember(JsonObject project, Guid userId)
    {
        var users = project["users"]!.AsArray();
        users.Should().Contain(node => node!["user"]!["id"]!.GetValue<Guid>() == userId,
            "user list " + users.ToJsonString());
    }

    [Fact]
    public async Task MemberCanSeeProjectMembers()
    {
        await LoginAs("manager");
        await using var project = await this.RegisterProjectInLexBox(Utils.GetNewProjectConfig());
        //refresh jwt
        await LoginAs("manager");
        var json = GetProject(await QueryProject(project.Code));
        MustHaveMembers(json);
    }

    [Fact]
    public async Task NonMemberCannotSeeProjectMembers()
    {
        await LoginAs("manager");
        await using var project = await this.RegisterProjectInLexBox(Utils.GetNewProjectConfig());
        await LoginAs("user");
        var json = GetProject(await QueryProject(project.Code));
        MustNotHaveMembers(json);
    }

    [Fact]
    public async Task ConfidentialProject_ManagerCanSeeProjectMembers()
    {
        await LoginAs("manager");
        await using var project = await this.RegisterProjectInLexBox(Utils.GetNewProjectConfig(isConfidential: true));
        await LoginAs("manager");
        var json = GetProject(await QueryProject(project.Code));
        MustHaveMembers(json);
    }

    [Fact]
    public async Task ConfidentialProject_NonManagerCannotSeeProjectMembers()
    {
        await LoginAs("manager");
        await using var project = await this.RegisterProjectInLexBox(Utils.GetNewProjectConfig(isConfidential: true));
        await LoginAs("manager");
        await AddUserToProject(project.Id, "editor");
        MustHaveMembers(GetProject(await QueryProject(project.Code)), count: 2);
        await LoginAs("editor");
        var json = GetProject(await QueryProject(project.Code));
        MustHaveOnlyUserAsMember(json, CurrentUser.Id);
    }

    [Fact]
    public async Task ConfidentialProject_NonMemberCannotSeeProject()
    {
        await LoginAs("manager");
        await using var project = await this.RegisterProjectInLexBox(Utils.GetNewProjectConfig(isConfidential: true));
        await LoginAs("user");
        var json = await QueryProject(project.Code, expectGqlError: true);
        var error = json["errors"]!.AsArray().First()?.AsObject();
        error.Should().NotBeNull();
        error["extensions"]?["code"]?.GetValue<string>().Should().Be("AUTH_NOT_AUTHORIZED");
    }
}
