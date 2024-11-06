using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using LexCore.Entities;
using Quartz.Util;
using Shouldly;
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

    public static ProjectConfig GetNewProjectConfig(HgProtocol? protocol = null, bool isConfidential = false, Guid? owningOrgId = null, [CallerMemberName] string projectName = "")
    {
        if (protocol.HasValue) projectName += $" ({protocol.Value.ToString()[..5]})";
        var id = Guid.NewGuid();
        var shortId = id.ToString().Split("-")[0];
        var projectCode = $"{ToProjectCodeFriendlyString(projectName)}-{shortId}-dev-flex";
        var dir = GetNewProjectDir(projectCode, "");
        return new ProjectConfig(id, projectName, projectCode, dir, isConfidential, owningOrgId);
    }

    public static async Task<LexboxProject> RegisterProjectInLexBox(
        ProjectConfig config,
        ApiTestBase apiTester,
        bool waitForRepoReady = false
    )
    {
        await apiTester.ExecuteGql($$"""
            mutation {
                createProject(input: {
                    name: "{{config.Name}}",
                    type: FL_EX,
                    id: "{{config.Id}}",
                    code: "{{config.Code}}",
                    isConfidential: {{config.IsConfidential.ToString().ToLowerInvariant()}},
                    description: "Project created by an integration test",
                    orgId: {{(config.OwningOrgId is null ? "null" : "\"" + config.OwningOrgId.ToString() + "\"")}}
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
        if (waitForRepoReady) await WaitForHgRefreshIntervalAsync();
        return new LexboxProject(apiTester, config.Id);
    }

    public static async Task AddMemberToProject(
        ProjectConfig config,
        ApiTestBase apiTester,
        string usernameOrEmail,
        ProjectRole role
    )
    {
        await apiTester.ExecuteGql($$"""
            mutation {
                addProjectMember(input: {
                    projectId: "{{config.Id}}",
                    usernameOrEmail: "{{usernameOrEmail}}"
                    role: {{role.ToString().ToUpper()}}
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
                        ... on InvalidEmailError {
                            address
                        }
                    }
                }
            }
            """);
    }

    public static void ValidateSendReceiveOutput(string srOutput)
    {
        srOutput.ShouldNotContain("abort");
        srOutput.ShouldNotContain("failure");
        srOutput.ShouldNotContain("error");
    }

    public static string ToProjectCodeFriendlyString(string name)
    {
        var dashesBeforeCapitals = Regex.Replace(name, "(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z0-9]|(?<=[0-9])[A-Z])", "-$1")
            .Trim().ToLower();
        var onlyLettersNumbersAndDashes = Regex.Replace(dashesBeforeCapitals, @"[^a-zA-Z0-9]+", "-");
        return onlyLettersNumbersAndDashes.Trim('-');
    }

    public static async Task WaitForHgRefreshIntervalAsync()
    {
        await Task.Delay(TestingEnvironmentVariables.HgRefreshInterval);
    }

    private static string GetNewProjectDir(string projectCode,
        [CallerMemberName] string projectName = "")
    {
        var projectDir = projectName.IsNullOrWhiteSpace() ? BasePath : Path.Join(BasePath, projectName);
        // Add a random id to the path to be certain we prevent naming clashes
        var randomIndexedId = $"{_folderIndex++}-{Guid.NewGuid().ToString().Split("-")[0]}";
        //fwdata file containing folder name will be the same as the file name
        projectDir = Path.Join(projectDir, randomIndexedId, projectCode);
        projectDir.Length.ShouldBeLessThan(150, $"Path may be too long with mercurial directories {projectDir}");
        return projectDir;
    }
}

public record LexboxProject : IAsyncDisposable
{
    public readonly Guid id;
    private readonly ApiTestBase _apiTester;
    private readonly string _jwt;

    public LexboxProject(ApiTestBase apiTester, Guid id)
    {
        this.id = id;
        _apiTester = apiTester;
        _jwt = apiTester.CurrJwt ?? throw new InvalidOperationException("No JWT found");
    }

    public async ValueTask DisposeAsync()
    {
        var response = await _apiTester.HttpClient.DeleteAsync($"api/project/{id}?jwt={_jwt}");
        response.EnsureSuccessStatusCode();
    }
}

public record ProjectPath(string Code, string Dir)
{
    public string FwDataFile { get; } = Path.Join(Dir, $"{Code}.fwdata");
}

public record ProjectConfig(Guid Id, string Name, string Code, string Dir, bool IsConfidential, Guid? OwningOrgId) : ProjectPath(Code, Dir)
{
    public ProjectConfig(Guid id, string name, ProjectPath config, bool isConfidential, Guid? owningOrgId) : this(id, name, config.Code, config.Dir, isConfidential, owningOrgId) { }
}
