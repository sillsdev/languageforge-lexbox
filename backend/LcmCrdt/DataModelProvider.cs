using CrdtLib;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace LcmCrdt;

public class DataModelProvider<TIdentifier>(
    IHttpContextAccessor contextAccessor,
    ProjectsService projectsService,
    IProjectProvider<TIdentifier> projectProvider)
{
    public async ValueTask<DataModel> GetDataModel(TIdentifier id)
    {
        var project = await projectProvider.GetProject(id);
        projectsService.SetProjectScope(project);
        return contextAccessor.HttpContext!.RequestServices.GetRequiredService<DataModel>();
    }
}
