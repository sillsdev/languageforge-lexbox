using LcmCrdt;
using MiniLcm.Models;

namespace FwLiteShared.Sync;

public interface IBackgroundSyncService
{
    void TriggerSync(Guid projectId, Guid? ignoredClientId = null);
    void TriggerSync(IProjectIdentifier project);
    void TriggerSync(CrdtProject crdtProject);
}
