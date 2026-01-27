# SyncWorkerTests plan (real SyncWorker)

Goal: replace the TestSyncWorker subclass with the real SyncWorker while still verifying
the orchestration call order and error handling using DI and mocks.

## Primary calls to verify (call order)

1. Pre sync: ISendReceiveService.SendReceive or ISendReceiveService.Clone
2. CrdtFwdataProjectSyncService.Sync
3. Post sync: ISendReceiveService.SendReceive
4. CrdtFwdataProjectSyncService.RegenerateProjectSnapshot
5. CrdtSyncService.SyncHarmonyProject

Notes:
- SyncHarmonyProject can also run before Crdt sync when
  CrdtFwdataProjectSyncService.HasSyncedSuccessfully(...) returns true.
  Ensure test setup chooses the path that matches the intended ordering.

## Secondary calls to verify

- CrdtHttpSyncService.TestAuth
- CrdtProjectsService.CreateProject
- IProjectMetadataService.BlockFromSyncAsync
- MediaFileService.SyncMediaFiles

## Test setup approach

- Build a ServiceCollection and register mocks for:
  - ISendReceiveService
  - CrdtFwdataProjectSyncService
  - CrdtSyncService
  - CrdtHttpSyncService
  - CrdtProjectsService
  - IProjectMetadataService
  - MediaFileService
  - IProjectLookupService
  - ISyncJobStatusService
- Use ActivatorUtilities.CreateInstance<SyncWorker>(serviceProvider, projectId)
  to run the real orchestration.
- For each mocked method, use Callback to append to a shared call list:
  - _callSequence.Add(nameof(ISendReceiveService.SendReceive))
  - _callSequence.Add(nameof(ISendReceiveService.Clone))
  - _callSequence.Add(nameof(CrdtFwdataProjectSyncService.Sync))
  - _callSequence.Add(nameof(CrdtFwdataProjectSyncService.RegenerateProjectSnapshot))
  - _callSequence.Add(nameof(CrdtSyncService.SyncHarmonyProject))
  - _callSequence.Add(nameof(CrdtHttpSyncService.TestAuth))
  - _callSequence.Add(nameof(CrdtProjectsService.CreateProject))
  - _callSequence.Add(nameof(IProjectMetadataService.BlockFromSyncAsync))
  - _callSequence.Add(nameof(MediaFileService.SyncMediaFiles))

## Per-test considerations

- Configure SyncResult to drive post-SR and snapshot conditions.
- Configure SendReceive/Clone results to drive rollback detection.
- Configure CrdtHttpSyncService.TestAuth to drive auth failure tests.
- Configure CrdtProjectsService.CreateProject when the crdt file is absent.
- Ensure FwDataProject file presence matches pre-SR path needed.
