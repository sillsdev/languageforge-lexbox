 using Crdt.Db;
 using LocalWebApp.Services;
 using Microsoft.OpenApi.Models;
using MiniLcm;

namespace LocalWebApp.Routes;

public static class ImportRoutes
{
    public static IEndpointConventionBuilder MapImport(this WebApplication app)
    {
        var group = app.MapGroup("/api/import/fwdata/{fwDataFile}");
        group.MapPost("/",
            async (string fwDataFile, ImportFwdataService importService) => await importService.Import(fwDataFile));
        return group;
    }
}
