using System.Text.Json.Nodes;
using LexCore.Entities;
using Shouldly;
using Testing.Fixtures;
using static Testing.Services.Utils;

namespace Testing.ApiTests;

[Trait("Category", "Integration")]
public class GqlMiddlewareTests : IClassFixture<IntegrationFixture>, IAsyncLifetime
{
    private readonly IntegrationFixture _fixture;
    private readonly ApiTestBase _adminApiTester;
    private string _adminJwt;

    public GqlMiddlewareTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
        _adminApiTester = _fixture.AdminApiTester;
        _adminJwt = _fixture.AdminJwt;
    }

    public async Task InitializeAsync()
    {
        if (_adminJwt != _adminApiTester.CurrJwt)
        {
            _adminJwt = await _adminApiTester.LoginAs("admin");
        }
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
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

        projects.Select(p => p.Id).ShouldBeSubsetOf(ids);
    }

    [Fact]
    public async Task CanGetProjectThatWasJustAddedToUser()
    {
        var config = GetNewProjectConfig(isConfidential: true);
        await using var project = await RegisterProjectInLexBox(config, _adminApiTester);

        await _adminApiTester.LoginAs("editor");
        var editorJwt = _adminApiTester.CurrJwt;

        await _adminApiTester.ExecuteGql($$"""
            query {
                projectByCode(code: "{{config.Code}}") {
                    id
                    name
                }
            }
            """, expectGqlError: true); // we're not a member yet
        _adminApiTester.CurrJwt.ShouldBe(editorJwt); // token wasn't updated

        await AddMemberToProject(config, _adminApiTester, "editor", ProjectRole.Editor, _adminJwt);

        await _adminApiTester.ExecuteGql($$"""
            query {
                projectByCode(code: "{{config.Code}}") {
                    id
                    name
                }
            }
            """, expectGqlError: true); // we're a member, but didn't query for users, so...
        _adminApiTester.CurrJwt.ShouldBe(editorJwt); // token wasn't updated

        var response = await _adminApiTester.ExecuteGql($$"""
            query {
                projectByCode(code: "{{config.Code}}") {
                    id
                    name
                    users {
                        id
                    }
                }
            }
            """, expectGqlError: false); // we queried for users, so...
        _adminApiTester.CurrJwt.ShouldNotBe(editorJwt); // token was updated
    }
}
