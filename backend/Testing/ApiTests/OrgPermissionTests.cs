using System.Text.Json.Nodes;
using LexData;
using FluentAssertions;

namespace Testing.ApiTests;

[Trait("Category", "Integration")]
public class OrgPermissionTests : ApiTestBase
{
    private async Task<JsonObject> QueryOrg(Guid orgId)
    {
        var json = await ExecuteGql(
            $$"""
              query {
                  orgById(orgId: "{{orgId}}") {
                      name
                      members {
                          id
                          role
                          user {
                              id
                              name
                              username
                              email
                          }
                      }
                      projects {
                        id
                        isConfidential
                      }
                  }
              }
              """);
        return json;
    }

    private static JsonObject GetOrg(JsonObject json)
    {
        var org = json["data"]?["orgById"]?.AsObject();
        org.ShouldNotBeNull();
        return org;
    }

    private void MustHaveOneMemberWithEmail(JsonNode org)
    {
        org["members"]!.AsArray().Where(m => m?["user"]?["email"]?.GetValue<string>() is { Length: > 0 })
            .ShouldNotBeNullOrEmpty();
    }
    private void MustNotHaveMemberWithEmail(JsonNode org)
    {
        org["members"]!.AsArray().Where(m => m?["user"]?["email"]?.GetValue<string>() is { Length: > 0 })
            .Should().BeEmpty();
    }

    private void MustHaveOneMemberWithUsername(JsonNode org)
    {
        org["members"]!.AsArray().Where(m => m?["user"]?["username"]?.GetValue<string>() is { Length: > 0 })
            .ShouldNotBeNullOrEmpty();
    }
    private void MustNotHaveMemberWithUsername(JsonNode org)
    {
        org["members"]!.AsArray().Where(m => m?["user"]?["username"]?.GetValue<string>() is { Length: > 0 })
            .Should().BeEmpty();
    }

    private void MustHaveUserNames(JsonNode org)
    {
        org["members"]!.AsArray()
            .Where(m => m?["user"]?["name"]?.GetValue<string>() is { Length: > 0 })
            .ShouldNotBeNullOrEmpty();
    }

    private void MustContainUser(JsonNode org, Guid id)
    {
        org["members"]!.AsArray().Should().Contain(
            m => m!["user"]!["id"]!.GetValue<Guid>() == id,
            $"org: '{org["name"]}' members were: {org["members"]!.ToJsonString()}");
    }

    private void MustHaveOnlyManagers(JsonNode org)
    {
        org["members"]!.AsArray()
            .Where(m => m?["role"]?.GetValue<string>() is not "ADMIN")
            .Should().BeEmpty();
    }

    private void MustHaveNonManagers(JsonNode org)
    {
        org["members"]!.AsArray()
            .Where(m => m?["role"]?.GetValue<string>() is not "ADMIN")
            .ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public async Task CanNotListOrgsAndListOrgUsers()
    {
        await LoginAs("manager");
        var json = await ExecuteGql(
            """
            query {
                orgs {
                    name
                    members {
                        id
                        role
                        user {
                            id
                            name
                        }
                    }
                }
            }
            """,
            true, false);
        var error = json["errors"]?.AsArray().First()?.AsObject();
        error.ShouldNotBeNull();
        error["extensions"]?["code"]?.GetValue<string>().Should().Be("AUTH_NOT_AUTHORIZED");
    }

    [Fact]
    public async Task CanNotListOrgsAndListOrgProjects()
    {
        await LoginAs("manager");
        var json = await ExecuteGql(
            """
            query {
                orgs {
                    name
                    projects {
                        id
                    }
                }
            }
            """,
            true, false);
        var error = json["errors"]?.AsArray().First()?.AsObject();
        error.ShouldNotBeNull();
        error["extensions"]?["code"]?.GetValue<string>().Should().Be("AUTH_NOT_AUTHORIZED");
    }

    [Fact]
    public async Task AdminCanQueryOrgMembers()
    {
        await LoginAs("admin");
        var org = GetOrg(await QueryOrg(SeedingData.TestOrgId));
        MustHaveOneMemberWithEmail(org);
    }

    [Fact]
    public async Task ManagerCanSeeMemberEmails()
    {
        await LoginAs("manager");
        var org = GetOrg(await QueryOrg(SeedingData.TestOrgId));
        MustHaveOneMemberWithEmail(org);
    }

    [Fact]
    public async Task ManagerCanSeeMemberUsernames()
    {
        await LoginAs("manager");
        var org = GetOrg(await QueryOrg(SeedingData.TestOrgId));
        MustHaveOneMemberWithUsername(org);
    }

    [Fact]
    public async Task OrgMemberCanSeeThemselvesInOrg()
    {
        await LoginAs("editor");
        var org = GetOrg(await QueryOrg(SeedingData.TestOrgId));
        org.ShouldNotBeNull();
        MustContainUser(org, SeedingData.EditorId);
    }

    [Fact]
    public async Task OrgMemberCanNotSeeMemberEmails()
    {
        await LoginAs("editor");
        var org = GetOrg(await QueryOrg(SeedingData.TestOrgId));
        org.ShouldNotBeNull();
        MustHaveUserNames(org);
        MustNotHaveMemberWithEmail(org);
    }

    [Fact]
    public async Task OrgMemberCanNotSeeMemberUsernames()
    {
        await LoginAs("editor");
        var org = GetOrg(await QueryOrg(SeedingData.TestOrgId));
        org.ShouldNotBeNull();
        MustHaveUserNames(org);
        MustNotHaveMemberWithUsername(org);
    }

    [Fact]
    public async Task NonMemberCanOnlyQueryManagers()
    {
        await LoginAs("user");
        var org = GetOrg(await QueryOrg(SeedingData.TestOrgId));
        MustHaveOnlyManagers(org);
        MustNotHaveMemberWithEmail(org);
    }

    private void MustNotShowConfidentialProjects(JsonNode org)
    {
        var projects = org["projects"]!.AsArray();
        projects.ShouldNotBeNullOrEmpty();
        projects
            .Where(p => p?["isConfidential"]?.GetValue<bool>() != false)
            .Should().BeEmpty();
    }

    private void MustContainProject(JsonNode org, Guid projectId)
    {
        var projects = org["projects"]!.AsArray();
        projects.ShouldNotBeNullOrEmpty();
        projects.Should().Contain(p => p!["id"]!.GetValue<Guid>() == projectId, $"project id '{projectId}' should exist in: {projects.ToJsonString()}");
    }

    private void MustNotContainProject(JsonNode org, Guid projectId)
    {
        var projects = org["projects"]!.AsArray();
        if ((projects?.Count ?? 0) == 0) return;
        projects!.Should().NotContain(p => p!["id"]!.GetValue<Guid>() == projectId, $"project id '{projectId}' should not exist in: {projects!.ToJsonString()}");
    }

    [Fact]
    public async Task NonMembersOnlySeePublicProjects()
    {
        await LoginAs("user");
        var org = GetOrg(await QueryOrg(SeedingData.TestOrgId));
        MustNotShowConfidentialProjects(org);
    }

    [Fact]
    public async Task MembersSeePublicAndTheirProjects()
    {
        await LoginAs("editor");
        var org = GetOrg(await QueryOrg(SeedingData.TestOrgId));
        MustContainProject(org, SeedingData.Sena3ProjId);
        MustContainProject(org, SeedingData.ElawaProjId);
        MustNotContainProject(org, SeedingData.EmptyProjId);
    }

    [Fact]
    public async Task ManagersSeeAllProjects()
    {
        await LoginAs("manager");
        var org = GetOrg(await QueryOrg(SeedingData.TestOrgId));
        MustContainProject(org, SeedingData.Sena3ProjId);
        MustContainProject(org, SeedingData.ElawaProjId);
        MustContainProject(org, SeedingData.EmptyProjId);
    }

    [Fact]
    public async Task NonMemberCanJustQueryOrgWithoutMembersOrProjects()
    {
        await LoginAs("user");
        await ExecuteGql(
            $$"""
              query {
                  orgById(orgId: "{{SeedingData.TestOrgId}}") {
                      name
                  }
              }
              """);
    }
}
