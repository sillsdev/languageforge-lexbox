using System.Text.Json.Nodes;
using FluentAssertions;
using Testing.Services;

namespace Testing.ApiTests;

[Trait("Category", "Integration")]
public class UsersICanSeeQueryTests : ApiTestBase
{
    private async Task<JsonObject> QueryUsersICanSee(bool expectGqlError = false)
    {
        var json = await ExecuteGql(
            $$"""
              query {
                usersICanSee(take: 10) {
                  totalCount
                  items {
                    id
                    name
                  }
                }
              }
              """,
            expectGqlError, expectSuccessCode: false);
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

    private JsonArray GetUsers(JsonObject json)
    {
        var users = json["data"]!["usersICanSee"]!["items"]!.AsArray();
        users.Should().NotBeNull();
        return users;
    }

    private void MustHaveUser(JsonArray users, string userName)
    {
        users.Should().NotBeNull().And.NotBeEmpty();
        users.Should().Contain(node => node!["name"]!.GetValue<string>() == userName,
            "user list " + users.ToJsonString());
    }

    private void MustNotHaveUser(JsonArray users, string userName)
    {
        users.Should().NotBeNull().And.NotBeEmpty();
        users.Should().NotContain(node => node!["name"]!.GetValue<string>() == userName,
            "user list " + users.ToJsonString());
    }

    [Fact]
    public async Task ManagerCanSeeProjectMembersOfAllProjects()
    {
        await LoginAs("manager");
        await using var project = await this.RegisterProjectInLexBox(Utils.GetNewProjectConfig(isConfidential: true));
        //refresh jwt
        await LoginAs("manager");
        await AddUserToProject(project.Id, "qa@test.com");
        var json = GetUsers(await QueryUsersICanSee());
        MustHaveUser(json, "Qa Admin");
    }

    [Fact]
    public async Task MemberCanSeeNotProjectMembersOfConfidentialProjects()
    {
        await LoginAs("manager");
        await using var project = await this.RegisterProjectInLexBox(Utils.GetNewProjectConfig(isConfidential: true));
        //refresh jwt
        await LoginAs("manager");
        await AddUserToProject(project.Id, "qa@test.com");
        await LoginAs("editor");
        var json = GetUsers(await QueryUsersICanSee());
        MustNotHaveUser(json, "Qa Admin");
    }
}
