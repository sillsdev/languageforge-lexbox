using System.Text.Json;
using FwLiteShared.Projects;
using LexCore.Entities;

namespace FwLiteShared.Tests.Projects;

public class ListProjectsJsonTests
{
    private static ListProjectsResult Parse(params string[] roles)
    {
        var projects = roles.Select(role =>
            $$"""{"id":"{{Guid.NewGuid()}}","code":"code","name":"name","isFwDataProject":false,"isCrdtProject":true,"role":"{{role}}"}""");
        var json = $$"""{"projects":[{{string.Join(",", projects)}}],"canDownloadByCode":true}""";
        return JsonSerializer.Deserialize<ListProjectsResult>(json, LexboxProjectService.ListProjectsJsonOptions)!;
    }

    [Fact]
    public void ReadsKnownRoles()
    {
        Parse("Editor", "Commenter", "observer").Projects.Select(p => p.Role)
            .Should().Equal(ProjectRole.Editor, ProjectRole.Commenter, ProjectRole.Observer);
    }

    [Fact]
    public void AnUnknownRoleDoesNotFailTheWholeList()
    {
        // A role added to the server after this client was released
        var result = Parse("Editor", "SomeFutureRole");

        result.Projects.Select(p => p.Role).Should().Equal(ProjectRole.Editor, ProjectRole.Unknown);
        result.CanDownloadByCode.Should().BeTrue();
    }

    [Fact]
    public void DefaultOptionsStillRejectAnUnknownRole()
    {
        // Guards the test above: the shared DTO's own converter throws, so the tolerance must come from the client options
        var json = $$"""{"projects":[{"id":"{{Guid.NewGuid()}}","code":"c","name":"n","isFwDataProject":false,"isCrdtProject":true,"role":"SomeFutureRole"}],"canDownloadByCode":true}""";

        var parse = () => JsonSerializer.Deserialize<ListProjectsResult>(json, JsonSerializerOptions.Web);

        parse.Should().Throw<JsonException>();
    }
}
