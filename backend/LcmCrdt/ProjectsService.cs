using Crdt.Db;
using Microsoft.Extensions.DependencyInjection;

namespace LcmCrdt;

public class ProjectsService(IServiceProvider provider, ProjectContext projectContext)
{
    public Task<CrdtProject[]> ListProjects()
    {
        return Task.FromResult(Directory.EnumerateFiles(".", "*.sqlite").Select(file =>
        {
            var name = Path.GetFileNameWithoutExtension(file);
            return new CrdtProject(name, file);
        }).ToArray());
    }

    public CrdtProject? GetProject(string name)
    {
        var file = Directory.EnumerateFiles(".", "*.sqlite")
            .FirstOrDefault(file => Path.GetFileNameWithoutExtension(file) == name);
        return file is null ? null : new CrdtProject(name, file);
    }

    public bool ProjectExists(string name)
    {
        return GetProject(name) is not null;
    }

    public async Task<CrdtProject> CreateProject(string name,
        Guid? id = null,
        string? domain = null,
        Func<IServiceProvider, CrdtProject, Task>? afterCreate = null,
        string? sqliteFile = null,
        CrdtDbContext? db = null)
    {
        sqliteFile ??= $"{name}.sqlite";
        if (File.Exists(sqliteFile)) throw new InvalidOperationException("Project already exists");
        var crdtProject = new CrdtProject(name, sqliteFile);
        using var serviceScope = CreateProjectScope(crdtProject);
        db ??= serviceScope.ServiceProvider.GetRequiredService<CrdtDbContext>();
        await db.Database.EnsureCreatedAsync();
        db.Set<ProjectData>().Add(new ProjectData(name, id ?? Guid.NewGuid(), domain, Guid.NewGuid()));
        await db.SaveChangesAsync();
        await (afterCreate?.Invoke(serviceScope.ServiceProvider, crdtProject) ?? Task.CompletedTask);
        return crdtProject;
    }

    public IServiceScope CreateProjectScope(CrdtProject crdtProject)
    {
        var serviceScope = provider.CreateScope();
        SetProjectScope(crdtProject);
        return serviceScope;
    }

    public void SetProjectScope(CrdtProject crdtProject)
    {
        projectContext.Project = crdtProject;
    }
}
