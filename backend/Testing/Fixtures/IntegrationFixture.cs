using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using LexCore.Utils;
using Shouldly;
using Testing.ApiTests;
using Testing.Services;
using TusDotNetClient;
using static Testing.Services.Constants;

namespace Testing.Fixtures;

public class IntegrationFixture : IAsyncLifetime
{
    private readonly string _templateRepoName = "test-template-repo.zip";
    public FileInfo TemplateRepoZip { get; } = new(Path.Join(BasePath, "_template-repo_.zip"));
    public DirectoryInfo TemplateRepo { get; } = new(Path.Join(BasePath, "_template-repo_"));
    public ApiTestBase AdminApiTester { get; } = new();
    private string AdminJwt = string.Empty;

    public async Task InitializeAsync()
    {
        DeletePreviousTestFiles();
        Directory.CreateDirectory(BasePath);
        InitTemplateRepo();
        AdminJwt = await AdminApiTester.LoginAs(AdminAuth.Username, AdminAuth.Password);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private static void DeletePreviousTestFiles()
    {
        if (Directory.Exists(BasePath)) Directory.Delete(BasePath, true);
    }

    private void InitTemplateRepo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var templatePath = Path.Join(Path.GetDirectoryName(assembly.Location), _templateRepoName);
        File.Copy(templatePath, TemplateRepoZip.FullName);
        using var stream = TemplateRepoZip.OpenRead();
        ZipFile.ExtractToDirectory(stream, TemplateRepo.FullName);
    }

    public ProjectConfig InitLocalFlexProjectWithRepo(HgProtocol? protocol = null, [CallerMemberName] string projectName = "")
    {
        var projectConfig = Utils.GetNewProjectConfig(protocol, projectName);
        InitLocalFlexProjectWithRepo(projectConfig);
        return projectConfig;
    }

    public void InitLocalFlexProjectWithRepo(ProjectPath projectPath)
    {
        var projectDir = Directory.CreateDirectory(projectPath.Dir);
        FileUtils.CopyFilesRecursively(TemplateRepo, projectDir);
        File.Move(Path.Join(projectPath.Dir, "kevin-test-01.fwdata"), projectPath.FwDataFile);
        Directory.EnumerateFiles(projectPath.Dir).ShouldContain(projectPath.FwDataFile);
    }

    public async Task FinishLexboxProjectResetWithTemplateRepo(string projectCode)
    {
        await FinishLexboxProjectResetWithRepo(projectCode, TemplateRepoZip);
    }

    public async Task FinishLexboxProjectResetWithRepo(string projectCode, FileInfo repo)
    {
        var client = new TusClient();
        client.AdditionalHeaders.Add("Cookie", $".LexBoxAuth={AdminJwt}");
        var fileUrl = await client.CreateAsync($"{AdminApiTester.BaseUrl}/api/project/upload-zip/{projectCode}", repo.Length, [("filetype", "application/zip")]);
        var responses = await client.UploadAsync(fileUrl, repo, chunkSize: 20);
        responses.ShouldAllBe(r => r.StatusCode.ToString() == nameof(HttpStatusCode.NoContent));
    }
}
