using System.IO.Compression;
using System.Runtime.CompilerServices;
using Chorus.VcsDrivers.Mercurial;
using LexCore.Utils;
using Shouldly;
using SIL.Progress;
using Testing.ApiTests;
using Testing.Services;
using static Testing.Services.Constants;

namespace Testing.Fixtures;

public class SendReceiveFixture : IAsyncLifetime
{
    private readonly DirectoryInfo _templateRepo = new(Path.Join(BasePath, "_template-repo_"));
    public ApiTestBase AdminApiTester { get; } = new();

    public async Task InitializeAsync()
    {
        DeletePreviousTestFiles();
        await DownloadTemplateRepo();
        await AdminApiTester.LoginAs(AdminAuth.Username, AdminAuth.Password);
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

        // hack around the fact that our send and receive won't create a repo from scratch.
        var progress = new NullProgress();
        HgRunner.Run("hg init", projectPath.Dir, 5, progress);
        HgRunner.Run("hg branch 7500002.7000072", projectPath.Dir, 5, progress);
        HgRunner.Run($"hg add Lexicon.fwstub", projectPath.Dir, 5, progress);
        HgRunner.Run("""hg commit -m "first commit" """, projectPath.Dir, 5, progress);
    }
}
