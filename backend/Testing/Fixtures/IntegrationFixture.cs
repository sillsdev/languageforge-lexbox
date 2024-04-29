using System.IO.Compression;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using LexCore.Utils;
using Shouldly;
using Squidex.Assets;
using Testing.ApiTests;
using Testing.Services;
using static Testing.Services.Constants;

namespace Testing.Fixtures;

public class IntegrationFixture : IAsyncLifetime
{
    private static readonly string _templateRepoName = "test-template-repo.zip";
    public FileInfo TemplateRepoZip { get; } = new(_templateRepoName);
    public DirectoryInfo TemplateRepo { get; } = new(Path.Join(BasePath, "_template-repo_"));
    public ApiTestBase AdminApiTester { get; private set; } = new();
    private string AdminJwt = string.Empty;

    public async Task InitializeAsync(ApiTestBase apiTester)
    {
        AdminApiTester = apiTester;
        await InitializeAsync();
    }

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
        if (TemplateRepo.Exists) return;
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
        Exception? failureException = null;
        await AdminApiTester.HttpClient.UploadWithProgressAsync(new Uri($"{AdminApiTester.BaseUrl}/api/project/upload-zip/{projectCode}"),
            UploadFile.FromFile(repo, "application/zip"), new UploadOptions
            {
                Metadata = new Dictionary<string, string> { ["filetype"] = "application/zip" },
                ProgressHandler = new DelegatingProgressHandler
                {
                    OnFailedAsync = (e, ct) =>
                    {
                        failureException = e.Exception;
                        return Task.CompletedTask;
                    }
                },
            });
        if (failureException is not null) throw failureException;
    }
}
