using System.IO.Compression;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.LcmUtils;
using FwDataMiniLcmBridge.Tests.Fixtures;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniLcm;
using SIL.IO;
using SIL.Progress;

namespace FwLiteProjectSync.Tests.Fixtures;

public class Sena3Fixture : IAsyncLifetime
{
    private readonly AsyncServiceScope _services;

    public CrdtFwdataProjectSyncService SyncService =>
        _services.ServiceProvider.GetRequiredService<CrdtFwdataProjectSyncService>();

    public IServiceProvider Services => _services.ServiceProvider;
    private readonly LexboxConfig lexboxConfig;
    private readonly HttpClient http;
    public CrdtMiniLcmApi CrdtApi { get; set; } = null!;
    public FwDataMiniLcmApi FwDataApi { get; set; } = null!;
    private bool AlreadyLoggedIn { get; set; } = false;

    public Sena3Fixture()
    {
        var services = new ServiceCollection()
            .AddSyncServices(nameof(Sena3Fixture), false);
        services.AddOptions<LexboxConfig>()
            .BindConfiguration("LexboxConfig")
            .Configure(c =>
            {
                // TODO: How do I set default values if and only if they're not already set (e.g., via environment variables)?
                c.LexboxUrl = "http://localhost/";
                c.LexboxUsername = "admin";
                c.LexboxPassword = "pass";
            })
            .ValidateDataAnnotations()
            .ValidateOnStart();
        _services = services.BuildServiceProvider().CreateAsyncScope();
        lexboxConfig = Services.GetRequiredService<IOptions<LexboxConfig>>().Value;
        var factory = Services.GetRequiredService<IHttpClientFactory>();
        http = factory.CreateClient(nameof(Sena3Fixture));
    }

    public async Task InitializeAsync()
    {
        var sena3MasterCopy = await DownloadSena3();
        var projectName = "sena-3_" + Guid.NewGuid().ToString("N");

        var projectsFolder = Services.GetRequiredService<IOptions<FwDataBridgeConfig>>().Value
            .ProjectsFolder;
        if (Path.Exists(projectsFolder)) Directory.Delete(projectsFolder, true);
        Directory.CreateDirectory(projectsFolder);
        var fwDataProject = new FwDataProject(projectName, projectsFolder);
        var fwDataProjectPath = Path.Combine(fwDataProject.ProjectsPath, fwDataProject.Name);
        DirectoryHelper.Copy(sena3MasterCopy, fwDataProjectPath);
        File.Move(Path.Combine(fwDataProjectPath, "sena-3.fwdata"), fwDataProject.FilePath);

        FwDataApi = Services.GetRequiredService<FwDataFactory>().GetFwDataMiniLcmApi(fwDataProject, false);

        var crdtProjectsFolder =
            Services.GetRequiredService<IOptions<LcmCrdtConfig>>().Value.ProjectPath;
        if (Path.Exists(crdtProjectsFolder)) Directory.Delete(crdtProjectsFolder, true);
        Directory.CreateDirectory(crdtProjectsFolder);
        var crdtProject = await Services.GetRequiredService<ProjectsService>()
            .CreateProject(new(projectName, FwProjectId: FwDataApi.ProjectId, SeedNewProjectData: false));
        CrdtApi = (CrdtMiniLcmApi) await Services.OpenCrdtProject(crdtProject);
    }

    public async Task DisposeAsync()
    {
        await _services.DisposeAsync();
    }

    public async Task<Stream> DownloadProjectBackupStream(string code)
    {
        var backupUrl = new Uri($"{lexboxConfig.LexboxUrl}api/project/backupProject/{code}");
        var result = await http.GetAsync(backupUrl);
        return await result.Content.ReadAsStreamAsync();
    }

    public async Task LoginAs(string lexboxUsername, string lexboxPassword)
    {
        if (AlreadyLoggedIn) return;
        await http.PostAsync($"{lexboxConfig.LexboxUrl}api/login", JsonContent.Create(new { EmailOrUsername=lexboxUsername, Password=lexboxPassword }));
        AlreadyLoggedIn = true;
    }

    public async Task<string> DownloadSena3()
    {
        var tempFolder = Path.Combine(Path.GetTempPath(), nameof(Sena3Fixture));
        var sena3MasterCopy = Path.Combine(tempFolder, "sena-3");
        if (!Directory.Exists(sena3MasterCopy) || !File.Exists(Path.Combine(sena3MasterCopy, "sena-3.fwdata")))
        {
            await LoginAs(lexboxConfig.LexboxUsername, lexboxConfig.LexboxPassword);
            Directory.CreateDirectory(sena3MasterCopy);
            await using var zipStream = await DownloadProjectBackupStream("sena-3");
            ZipFile.ExtractToDirectory(zipStream, sena3MasterCopy);
            MercurialTestHelper.HgUpdate(sena3MasterCopy, "tip");
            LfMergeBridge.LfMergeBridge.ReassembleFwdataFile(new NullProgress(), false, Path.Combine(sena3MasterCopy, "sena-3.fwdata"));
            MercurialTestHelper.HgClean(sena3MasterCopy, "sena-3.fwdata");
        }
        return sena3MasterCopy;
    }
}
