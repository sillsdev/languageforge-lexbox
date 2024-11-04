using System.Text.Json.Nodes;
using LexCore.Entities;
using Shouldly;
using Testing.Fixtures;
using static Testing.Services.Utils;

namespace Testing.ApiTests;

[Trait("Category", "Integration")]
public class GqlMiddlewareTests : IClassFixture<IntegrationFixture>
{
    private readonly IntegrationFixture _fixture;
    private readonly ApiTestBase _adminApiTester;

    public GqlMiddlewareTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
        _adminApiTester = _fixture.AdminApiTester;
    }

    private async Task<JsonObject> QueryMyProjectsWithMembers()
    {
        var json = await _adminApiTester.ExecuteGql(
            $$"""
            query loadMyProjects {
                myProjects(orderBy: [ { name: ASC } ]) {
                    code
                    id
                    name
                    users {
                        id
                        userId
                        role
                    }
                }
            }
            """);
        return json;
    }

    [Fact]
    public async Task CanTriggerMultipleInstancesOfMiddlewareThatAccessDbSimultaneously()
    {
        var config1 = GetNewProjectConfig();
        var config2 = GetNewProjectConfig();
        var config3 = GetNewProjectConfig();

        var projects = await Task.WhenAll(
            RegisterProjectInLexBox(config1, _adminApiTester),
            RegisterProjectInLexBox(config2, _adminApiTester),
            RegisterProjectInLexBox(config3, _adminApiTester));

        await using var project1 = projects[0];
        await using var project2 = projects[1];
        await using var project3 = projects[2];

        await Task.WhenAll(
            AddMemberToProject(config1, _adminApiTester, "editor", ProjectRole.Editor),
            AddMemberToProject(config2, _adminApiTester, "editor", ProjectRole.Editor),
            AddMemberToProject(config3, _adminApiTester, "editor", ProjectRole.Editor));

        await _adminApiTester.LoginAs("editor");
        // Because we assigned ProjectRole.Editor and these projects are new,
        // our middlware will query the project confidentiality from the DB to determine
        // if the user is allowed to view all members
        var json = await QueryMyProjectsWithMembers();

        json.ShouldNotBeNull();
        var myProjects = json["data"]!["myProjects"]!.AsArray();
        var ids = myProjects.Select(p => p!["id"]!.GetValue<Guid>());

        projects.Select(p => p.id).ShouldBeSubsetOf(ids);
    }
}
