using FwDataMiniLcmBridge;
using LcmCrdt;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm.Models;

namespace FwLiteProjectSync.Tests.Fixtures;

/// <summary>
/// A CRDT project on its own, plus the file paths the merge base and sync journal live at. For tests about the merge
/// base and the sync staging area, which never touch fwdata and shouldn't pay for loading it.
/// </summary>
public sealed class CrdtOnlyProjectFixture : IAsyncDisposable
{
    /// <summary>Matches FwHeadless's appsettings; the merge base check recognises the sync's commits by this name.</summary>
    public const string SyncAuthor = "FieldWorks";

    private readonly ServiceProvider _root;
    private readonly List<AsyncServiceScope> _scopes = [];
    private AsyncServiceScope _scope;

    private CrdtOnlyProjectFixture(ServiceProvider root, string projectFolder)
    {
        _root = root;
        ProjectFolder = projectFolder;
    }

    public string ProjectFolder { get; }
    public CrdtProject CrdtProject { get; private set; } = null!;
    public CrdtMiniLcmApi CrdtApi { get; private set; } = null!;

    /// <summary>Only its paths matter here: the merge base and the sync journal sit next to a project's fwdata.</summary>
    public FwDataProject FwDataProject { get; private set; } = null!;

    public IServiceProvider Services => _scope.ServiceProvider;

    public static async Task<CrdtOnlyProjectFixture> Create(string testName)
    {
        var projectFolder = Path.Combine(".", testName, Guid.NewGuid().ToString("N")[..8]);
        var root = new ServiceCollection()
            .AddSyncServices(projectFolder)
            .Configure<LcmCrdtConfig>(c => c.DefaultAuthorForCommits = SyncAuthor)
            .BuildServiceProvider();
        var fixture = new CrdtOnlyProjectFixture(root, projectFolder);
        await fixture.Initialize();
        return fixture;
    }

    private async Task Initialize()
    {
        Directory.CreateDirectory(Path.Combine(ProjectFolder, "LcmCrdt"));
        _scope = _root.CreateAsyncScope();
        CrdtProject = await _scope.ServiceProvider.GetRequiredService<CrdtProjectsService>()
            .CreateProject(new("crdt", "crdt", FwProjectId: Guid.NewGuid()));
        CrdtApi = (CrdtMiniLcmApi)await _scope.ServiceProvider.OpenCrdtProject(CrdtProject);
        // Entry queries need a default vernacular writing system, so a project without one can't produce a merge base.
        await CrdtApi.CreateWritingSystem(new WritingSystem
        {
            Id = Guid.NewGuid(),
            WsId = "en",
            Name = "English",
            Abbreviation = "en",
            Font = "Arial",
            Type = WritingSystemType.Vernacular
        });
        FwDataProject = new FwDataProject("fw", Path.GetDirectoryName(CrdtProject.DbPath)!);
    }

    /// <summary>Reads the project's database through a fresh scope, so a swapped-in file is actually re-opened.</summary>
    public async Task<CrdtMiniLcmApi> OpenFresh()
    {
        var scope = _root.CreateAsyncScope();
        _scopes.Add(scope);
        return (CrdtMiniLcmApi)await scope.ServiceProvider.OpenCrdtProject(new CrdtProject(CrdtProject.Name, CrdtProject.DbPath));
    }

    /// <summary>
    /// Writes one commit. <paramref name="authorName"/> stands in for a person's edit; the default is the sync's own
    /// author, which is how the merge base check tells the two apart. The interceptor has to come from the same scope
    /// as <paramref name="api"/>, hence <paramref name="apiServices"/>.
    /// </summary>
    public async Task CreatePartOfSpeech(CrdtMiniLcmApi api, string name, string? authorName = null, IServiceProvider? apiServices = null)
    {
        var partOfSpeech = new PartOfSpeech { Id = Guid.NewGuid(), Name = new MultiString { { "en", name } } };
        if (authorName is null)
        {
            await api.CreatePartOfSpeech(partOfSpeech);
            return;
        }

        var interceptor = (apiServices ?? Services).GetRequiredService<CommitMetadataInterceptor>();
        using (interceptor.Intercept(metadata => metadata.AuthorName = authorName))
        {
            await api.CreatePartOfSpeech(partOfSpeech);
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var scope in _scopes) await scope.DisposeAsync();
        await _scope.DisposeAsync();
        await _root.DisposeAsync();
        try { Directory.Delete(ProjectFolder, true); } catch { /* leftover temp files aren't worth failing a test */ }
    }
}
