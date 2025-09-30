using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Api;
using LcmCrdt;

namespace FwLiteProjectSync.Tests.Fixtures;

public record TestProject(
    CrdtMiniLcmApi CrdtApi, FwDataMiniLcmApi FwDataApi,
    CrdtProject CrdtProject, FwDataProject FwDataProject,
    IServiceProvider Services, IDisposable _cleanup) : IDisposable
{
    public void Dispose() { _cleanup.Dispose(); }
}
