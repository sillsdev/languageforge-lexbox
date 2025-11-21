using System.IO.Compression;
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Api;
using LcmCrdt;
using LexCore.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SIL.IO;
using SIL.Progress;

namespace FwLiteProjectSync.Tests.Fixtures;

public class Sena3Fixture : IAsyncLifetime
{
    private static readonly HttpClient http = new HttpClient();

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection()
            .AddSyncServices(nameof(Sena3Fixture), false);
        var rootServiceProvider = services.BuildServiceProvider();
        var fwProjectsFolder = rootServiceProvider.GetRequiredService<IOptions<FwDataBridgeConfig>>()
            .Value
            .ProjectsFolder;
        if (Path.Exists(fwProjectsFolder)) Directory.Delete(fwProjectsFolder, true);
        Directory.CreateDirectory(fwProjectsFolder);

        var crdtProjectsFolder =
            rootServiceProvider.GetRequiredService<IOptions<LcmCrdtConfig>>().Value.ProjectPath;
        if (Path.Exists(crdtProjectsFolder)) Directory.Delete(crdtProjectsFolder, true);
        await rootServiceProvider.DisposeAsync();

        Directory.CreateDirectory(crdtProjectsFolder);
        await DownloadSena3();
    }

    public async Task<TestProject> SetupProjects()
    {
        var sena3MasterCopy = await DownloadSena3();

        var rootServiceProvider = new ServiceCollection()
            .AddSyncServices(nameof(Sena3Fixture), false)
            .BuildServiceProvider();
        var cleanup = Defer.Action(() => rootServiceProvider.Dispose());
        var services = rootServiceProvider.CreateAsyncScope().ServiceProvider;
        var projectName = "sena-3_" + Guid.NewGuid().ToString("N");

        var projectsFolder = services.GetRequiredService<IOptions<FwDataBridgeConfig>>()
            .Value
            .ProjectsFolder;
        var fwDataProject = new FwDataProject(projectName, projectsFolder);
        DirectoryHelper.Copy(sena3MasterCopy, fwDataProject.ProjectFolder);
        File.Move(Path.Combine(fwDataProject.ProjectFolder, "sena-3.fwdata"), fwDataProject.FilePath);
        var fwDataMiniLcmApi = services.GetRequiredService<FwDataFactory>().GetFwDataMiniLcmApi(fwDataProject, false);

        var crdtProject = await services.GetRequiredService<CrdtProjectsService>()
            .CreateProject(new(projectName, projectName, FwProjectId: fwDataMiniLcmApi.ProjectId, SeedNewProjectData: false));
        var crdtMiniLcmApi = (CrdtMiniLcmApi)await services.OpenCrdtProject(crdtProject);
        return new TestProject(crdtMiniLcmApi, fwDataMiniLcmApi, crdtProject, fwDataProject, services, cleanup);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private async Task<Stream> DownloadSena3ProjectBackupStream()
    {
        var backupUrl = new Uri("https://drive.google.com/uc?export=download&id=1I-hwc0RHoQqW774gbS5qR-GHa1E7BlsS");
        var result = await http.GetAsync(backupUrl, HttpCompletionOption.ResponseHeadersRead);
        return await result.Content.ReadAsStreamAsync();
    }

    private async Task<string> DownloadSena3()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), nameof(Sena3Fixture));
        var sena3MasterCopy = Path.Combine(tempFolder, "sena-3");
        if (!Directory.Exists(sena3MasterCopy) || !File.Exists(Path.Combine(sena3MasterCopy, "sena-3.fwdata")))
        {
            Directory.CreateDirectory(sena3MasterCopy);
            await using var zipStream = await DownloadSena3ProjectBackupStream();
            //the zip file is structured like this: /sena-3/.hg
            //by extracting it to tempFolder it should merge with sena-3
            ZipFile.ExtractToDirectory(zipStream, tempFolder);

            MercurialTestHelper.HgUpdate(sena3MasterCopy, "tip");
            LfMergeBridge.LfMergeBridge.ReassembleFwdataFile(new NullProgress(), false, Path.Combine(sena3MasterCopy, "sena-3.fwdata"));
            MercurialTestHelper.HgClean(sena3MasterCopy, "sena-3.fwdata");
        }
        return sena3MasterCopy;
    }
}
