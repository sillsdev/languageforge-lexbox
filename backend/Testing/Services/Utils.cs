using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Chorus.VcsDrivers.Mercurial;
using LexCore.Utils;
using Shouldly;
using SIL.Progress;
using Testing.ApiTests;
using static Testing.Services.Constants;

namespace Testing.Services;

public static class Utils
{
    private static int _folderIndex = 1;

    public static SendReceiveParams GetParams(HgProtocol protocol,
        string? projectCode = null,
        [CallerMemberName] string projectName = "")
    {
        projectCode ??= TestingEnvironmentVariables.ProjectCode;
        var sendReceiveParams = new SendReceiveParams(projectCode, protocol.GetTestHostName(),
            GetNewProjectDir(projectCode, projectName));
        return sendReceiveParams;
    }

    public static ProjectConfig GetNewProjectConfig([CallerMemberName] string projectName = "")
    {
        var id = Guid.NewGuid();
        var projectCode = ToProjectCodeFriendlyString(projectName);
        var shortId = id.ToString().Split("-")[0];
        projectCode = $"{projectCode}-{shortId}-dev-flex";
        var dir = GetNewProjectDir(projectCode, projectName);
        return new ProjectConfig(id, projectName, projectCode, dir);
    }

    public static async Task<LexboxProject> RegisterProjectInLexBox(
        ProjectConfig config,
        ApiTestBase apiTester
    )
    {
        await apiTester.ExecuteGql($$"""
            mutation {
                createProject(input: {
                    name: "{{config.Name}}",
                    type: FL_EX,
                    id: "{{config.Id}}",
                    code: "{{config.Code}}",
                    description: "Project created by an integration test",
                    retentionPolicy: DEV
                }) {
                    createProjectResponse {
                        id
                        result
                    }
                    errors {
                        __typename
                        ... on DbError {
                            code
                            message
                        }
                    }
                }
            }
            """);
        return new LexboxProject(apiTester, config.Id);
    }

    public static void ValidateSendReceiveOutput(string srOutput)
    {
        srOutput.ShouldNotContain("abort");
        srOutput.ShouldNotContain("failure");
        srOutput.ShouldNotContain("error");
    }

    public static string ToProjectCodeFriendlyString(string name)
    {
        var dashesBeforeCapitals = Regex.Replace(name, "(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])", "-$1")
            .Trim().ToLower();
        var onlyLettersNumbersAndDashes = Regex.Replace(dashesBeforeCapitals, @"[^a-zA-Z0-9-]", "-");
        return onlyLettersNumbersAndDashes.Trim('-');
    }

    private static string GetNewProjectDir(string projectCode,
        [CallerMemberName] string projectName = "")
    {
        var projectDir = Path.Join(BasePath, projectName);
        // Add a random id to the path to be certain we prevent naming clashes
        var randomIndexedId = $"{_folderIndex++}-{Guid.NewGuid().ToString().Split("-")[0]}";
        //fwdata file containing folder name will be the same as the file name
        projectDir = Path.Join(projectDir, randomIndexedId, projectCode);
        projectDir.Length.ShouldBeLessThan(150, "Path may be too long with mercurial directories");
        return projectDir;
    }
}

public record LexboxProject : IAsyncDisposable
{
    private readonly Func<Task> delete;

    public LexboxProject(ApiTestBase apiTester, Guid id)
    {
        delete = async () =>
        {
            var response = await apiTester.HttpClient.DeleteAsync($"api/project/project/{id}");
            response.EnsureSuccessStatusCode();
        };
    }

    public async ValueTask DisposeAsync()
    {
        await delete();
    }
}

public record ProjectPath(string Code, string Dir)
{
    public string FwDataFile { get; } = Path.Join(Dir, $"{Code}.fwdata");
}

public record ProjectConfig(Guid Id, string Name, string Code, string Dir) : ProjectPath(Code, Dir)
{
    public ProjectConfig(Guid id, string name, ProjectPath config) : this(id, name, config.Code, config.Dir) { }
}
