using System.IO.Compression;
using System.Runtime.CompilerServices;
using LexCore.Utils;
using FluentAssertions;
using Squidex.Assets;
using Testing.ApiTests;
using Testing.Services;
using static Testing.Services.Constants;

namespace Testing.Fixtures;

public class IntegrationFixture : IAsyncLifetime
{
    private const string TemplateRepoZipName = "test-template-repo.zip";
    public static readonly FileInfo TemplateRepoZip = new(TemplateRepoZipName);
    public static readonly DirectoryInfo TemplateRepo = new(Path.Join(BasePath, "_template-repo_"));
    public ApiTestBase AdminApiTester { get; private set; } = new();
    private string? _adminJwt = null;
    public string AdminJwt => _adminJwt.Should().NotBeNull().And.Subject;

    static IntegrationFixture()
    {
        DeletePreviousTestFiles();
    }

    public async Task InitializeAsync(ApiTestBase apiTester)
    {
        AdminApiTester = apiTester;
        await InitializeAsync();
    }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(BasePath);
        InitTemplateRepo();
        _adminJwt = await AdminApiTester.LoginAs(AdminAuth.Username, AdminAuth.Password);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private static void DeletePreviousTestFiles()
    {
        if (Directory.Exists(BasePath)) Directory.Delete(BasePath, true);
    }

    private static void InitTemplateRepo()
    {
        lock (TemplateRepo)
        {
            if (TemplateRepo.Exists) return;
            using var stream = TemplateRepoZip.OpenRead();
            ZipFile.ExtractToDirectory(stream, TemplateRepo.FullName);
            TemplateRepo.Refresh();
        }
    }

    public ProjectConfig InitLocalFlexProjectWithRepo(HgProtocol? protocol = null, bool isConfidential = false, Guid? owningOrgId = null, [CallerMemberName] string projectName = "")
    {
        var projectConfig = Utils.GetNewProjectConfig(protocol, isConfidential, owningOrgId, projectName);
        InitLocalFlexProjectWithRepo(projectConfig);
        return projectConfig;
    }

    public void InitLocalFlexProjectWithRepo(ProjectPath projectPath)
    {
        var projectDir = Directory.CreateDirectory(projectPath.Dir);
        FileUtils.CopyFilesRecursively(TemplateRepo, projectDir);
        File.Move(Path.Join(projectPath.Dir, "kevin-test-01.fwdata"), projectPath.FwDataFile);
        Directory.EnumerateFiles(projectPath.Dir).Should().Contain(projectPath.FwDataFile);
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
