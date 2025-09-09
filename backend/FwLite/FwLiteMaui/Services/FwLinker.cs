#if INCLUDE_FWDATA_BRIDGE
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.LcmUtils;
using FwLiteShared.Services;
using Microsoft.JSInterop;

namespace FwLiteMaui.Services;

public class FwLinker(FwDataFactory fwDataFactory, FieldWorksProjectList projectList) : IFwLinker
{
    public string? GetLinkToEntry(Guid entryId, string projectName)
    {
        var project = projectList.GetProject(projectName);
        if (project is null) return null;
        fwDataFactory.CloseProject(project);
        return FwLink.ToEntry(entryId, projectName);
    }
}
#endif
