namespace Testing.FwHeadless;

/// <summary>
/// Collection for tests that trigger FwHeadless sync operations.
/// These tests are run serially because FwHeadless syncs are queued on the server
/// and running them in parallel causes timeouts.
/// </summary>
[CollectionDefinition("FwHeadless Sync", DisableParallelization = true)]
public class FwHeadlessSyncCollection
{
}
