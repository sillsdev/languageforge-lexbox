 using SIL.Harmony.Db;
 using LocalWebApp.Services;
 using Microsoft.OpenApi.Models;
using MiniLcm;

namespace LocalWebApp.Routes;

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
