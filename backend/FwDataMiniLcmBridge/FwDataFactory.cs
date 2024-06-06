using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.LcmUtils;
using Microsoft.Extensions.Logging;
using MiniLcm;

namespace FwDataMiniLcmBridge;

public class FwDataFactory(FwDataProjectContext context, ILogger<FwDataMiniLcmApi> logger)
{
    public FwDataMiniLcmApi GetFwDataMiniLcmApi(string projectName, bool saveOnDispose)
    {
        var project = FieldWorksProjectList.GetProject(projectName) ?? throw new InvalidOperationException($"Project {projectName} not found.");
        return GetFwDataMiniLcmApi(project, saveOnDispose);
    }
    public FwDataMiniLcmApi GetFwDataMiniLcmApi(FwDataProject project, bool saveOnDispose)
    {
        var lcmCache = ProjectLoader.LoadCache(project.FileName);
        return new FwDataMiniLcmApi(lcmCache, saveOnDispose, logger);
    }

    public FwDataMiniLcmApi GetCurrentFwDataMiniLcmApi()
    {
        var fwDataProject = context.Project;
        if (fwDataProject is null)
        {
            throw new InvalidOperationException("No project is set in the context.");
        }
        return GetFwDataMiniLcmApi(fwDataProject, true);
    }
}
