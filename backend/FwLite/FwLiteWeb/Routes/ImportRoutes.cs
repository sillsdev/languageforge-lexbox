 using FwLiteShared.Projects;
 using SIL.Harmony.Db;
 using FwLiteWeb.Services;
 using Microsoft.OpenApi.Models;
using MiniLcm;

namespace FwLiteWeb.Routes;

public static class ImportRoutes
{
    public static IEndpointConventionBuilder MapImport(this WebApplication app)
    {
        var group = app.MapGroup("/api/import");
        group.MapPost("/fwdata/{fwDataProjectName}",
            async (string fwDataProjectName, ImportFwdataService importService) => await importService.Import(fwDataProjectName));
        return group;
    }
}
