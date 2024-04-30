 using CrdtLib;

namespace LocalWebApp;

public class SyncService(DataModel dataModel, ISyncHttp remoteSyncServer)
{
    public async Task ExecuteSync()
    {
        await dataModel.SyncWith(remoteSyncServer);
    }
}
