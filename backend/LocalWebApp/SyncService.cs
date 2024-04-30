 using CrdtLib;

namespace LocalWebApp;

public class SyncService(DataModel dataModel, CrdtHttpSync remoteSyncServer)
{
    public async Task ExecuteSync()
    {
        await dataModel.SyncWith(remoteSyncServer);
    }
}
