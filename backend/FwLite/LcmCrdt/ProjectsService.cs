using SIL.Harmony;
using SIL.Harmony.Db;
using LcmCrdt.Utils;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniLcm;
using PartOfSpeech = LcmCrdt.Objects.PartOfSpeech;

namespace LcmCrdt;

public class ProjectsService(IServiceProvider provider, ProjectContext projectContext, ILogger<ProjectsService> logger, IOptions<LcmCrdtConfig> config)
{
    public Task<CrdtProject[]> ListProjects()
    {
        return Task.FromResult(Directory.EnumerateFiles(config.Value.ProjectPath, "*.sqlite").Select(file =>
        {
            var name = Path.GetFileNameWithoutExtension(file);
            return new CrdtProject(name, file);
        }).ToArray());
    }

    public CrdtProject? GetProject(string name)
    {
        var file = Directory.EnumerateFiles(config.Value.ProjectPath, "*.sqlite")
            .FirstOrDefault(file => Path.GetFileNameWithoutExtension(file) == name);
        return file is null ? null : new CrdtProject(name, file);
    }

    public bool ProjectExists(string name)
    {
        return GetProject(name) is not null;
    }

    public record CreateProjectRequest(
        string Name,
        Guid? Id = null,
        Uri? Domain = null,
        Func<IServiceProvider, CrdtProject, Task>? AfterCreate = null,
        bool SeedNewProjectData = true);

    public async Task<CrdtProject> CreateProject(CreateProjectRequest request)
    {
        //poor man's sanitation
        var name = Path.GetFileName(request.Name);
        var sqliteFile = Path.Combine(config.Value.ProjectPath, $"{name}.sqlite");
        if (File.Exists(sqliteFile)) throw new InvalidOperationException("Project already exists");
        var crdtProject = new CrdtProject(name, sqliteFile);
        await using var serviceScope = CreateProjectScope(crdtProject);
        var db = serviceScope.ServiceProvider.GetRequiredService<LcmCrdtDbContext>();
        try
        {
            var projectData = new ProjectData(name,
                request.Id ?? Guid.NewGuid(),
                ProjectData.GetOriginDomain(request.Domain),
                Guid.NewGuid());
            await InitProjectDb(db, projectData);
            await serviceScope.ServiceProvider.GetRequiredService<CurrentProjectService>().PopulateProjectDataCache();
            if (request.SeedNewProjectData)
                await SeedSystemData(serviceScope.ServiceProvider.GetRequiredService<DataModel>(), projectData.ClientId);
            await (request.AfterCreate?.Invoke(serviceScope.ServiceProvider, crdtProject) ?? Task.CompletedTask);
        }
        catch
        {
            logger.LogError("Failed to create project {Project}, deleting database", crdtProject.Name);
            await db.Database.EnsureDeletedAsync();
            throw;
        }
        return crdtProject;
    }

    internal static async Task InitProjectDb(LcmCrdtDbContext db, ProjectData data)
    {
        await db.Database.EnsureCreatedAsync();
        db.ProjectData.Add(data);
        await db.SaveChangesAsync();
    }

    internal static async Task SeedSystemData(DataModel dataModel, Guid clientId)
    {
        await PartOfSpeech.PredefinedPartsOfSpeech(dataModel, clientId);
    }

    public AsyncServiceScope CreateProjectScope(CrdtProject crdtProject)
    {
        var serviceScope = provider.CreateAsyncScope();
        SetProjectScope(crdtProject);
        return serviceScope;
    }

    public void SetProjectScope(CrdtProject crdtProject)
    {
        projectContext.Project = crdtProject;
    }

    public void SetActiveProject(string name)
    {
        var project = GetProject(name) ?? throw new InvalidOperationException($"Crdt Project {name} not found");
        SetProjectScope(project);
    }
}
