using FwDataMiniLcmBridge;

namespace FwHeadless.Services;

public interface ISendReceiveService
{
    Task<SendReceiveHelpers.LfMergeBridgeResult> SendReceive(FwDataProject project, string? projectCode, string? commitMessage = null);
    Task<SendReceiveHelpers.LfMergeBridgeResult> Clone(FwDataProject project, string? projectCode);
    Task<int> PendingCommitCount(FwDataProject project, string? projectCode);
    Task CommitFile(string filePath, string commitMessage);
}
