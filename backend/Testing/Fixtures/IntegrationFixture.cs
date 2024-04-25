using System.IO.Compression;
using System.Net;
using System.Runtime.CompilerServices;
using Chorus.VcsDrivers.Mercurial;
using LexCore.Utils;
using Shouldly;
using SIL.Linq;
using SIL.Progress;
using Testing.ApiTests;
using Testing.Services;
using TusDotNetClient;
using static Testing.Services.Constants;

namespace Testing.Fixtures;

public class IntegrationFixture : IAsyncLifetime
{
    private readonly FileInfo _templateRepoZip = new(Path.Join(BasePath, "_template-repo_.zip"));
    private readonly DirectoryInfo _templateRepo = new(Path.Join(BasePath, "_template-repo_"));
    public ApiTestBase AdminApiTester { get; } = new();
    private string AdminJwt = string.Empty;

    public async Task InitializeAsync()
    {
        DeletePreviousTestFiles();
        Directory.CreateDirectory(BasePath);
        await DownloadTemplateRepo();
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

    private async Task DownloadTemplateRepo()
    {
        await using var stream = await AdminApiTester.HttpClient.GetStreamAsync("https://drive.google.com/uc?export=download&id=1w357T1Ti7bDwEof4HPBUZ5gB7WSKA5O2");
        using var zip = new ZipArchive(stream);
        zip.ExtractToDirectory(_templateRepo.FullName);
        InitRepoInDirectory(_templateRepo.FullName);
        ZipFile.CreateFromDirectory(_templateRepo.FullName, _templateRepoZip.FullName);
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
        FileUtils.CopyFilesRecursively(_templateRepo, projectDir);
        File.Move(Path.Join(projectPath.Dir, "kevin-test-01.fwdata"), projectPath.FwDataFile);
        Directory.EnumerateFiles(projectPath.Dir).ShouldContain(projectPath.FwDataFile);
    }

    public async Task FinishLexboxProjectResetWithTemplateRepo(string projectCode)
    {
        await FinishLexboxProjectResetWithRepo(projectCode, _templateRepoZip);
    }

    public async Task FinishLexboxProjectResetWithRepo(string projectCode, FileInfo repo)
    {
        var client = new TusClient();
        client.AdditionalHeaders.Add("Cookie", $".LexBoxAuth={AdminJwt}");
        var fileUrl = await client.CreateAsync($"{AdminApiTester.BaseUrl}/api/project/upload-zip/{projectCode}", repo.Length, [("filetype", "application/zip")]);
        var responses = await client.UploadAsync(fileUrl, repo, chunkSize: 20);
        responses.ShouldAllBe(r => r.StatusCode.ToString() == nameof(HttpStatusCode.NoContent));
    }

    private static void InitRepoInDirectory(string projectDir)
    {
        var progress = new NullProgress();
        HgRunner.Run("hg init", projectDir, 5, progress);
        HgRunner.Run("hg branch 7500002.7000072", projectDir, 5, progress);
        HgRunner.Run($"hg add Lexicon.fwstub", projectDir, 5, progress);
        HgRunner.Run("""hg commit -m "first commit" """, projectDir, 5, progress);
    }
}
