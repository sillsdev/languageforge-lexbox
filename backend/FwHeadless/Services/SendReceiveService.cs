using FwDataMiniLcmBridge;
using Microsoft.Extensions.Options;

namespace FwHeadless.Services;

public class SendReceiveService(IOptions<FwHeadlessConfig> config, SafeLoggingProgress progress) : ISendReceiveService
{
    public async Task<SendReceiveHelpers.LfMergeBridgeResult> SendReceive(FwDataProject project, string? projectCode, string? commitMessage = null)
    {
        return await SendReceiveHelpers.SendReceive(
            project: project,
            projectCode: projectCode,
            baseUrl: config.Value.HgWebUrl,
            auth: new SendReceiveHelpers.SendReceiveAuth(config.Value),
            fdoDataModelVersion: config.Value.FdoDataModelVersion,
            commitMessage: commitMessage,
            progress: progress
        );
    }

    public async Task<SendReceiveHelpers.LfMergeBridgeResult> Clone(FwDataProject project, string? projectCode)
    {
        return await SendReceiveHelpers.CloneProject(
            project: project,
            projectCode: projectCode,
            baseUrl: config.Value.HgWebUrl,
            auth: new SendReceiveHelpers.SendReceiveAuth(config.Value),
            fdoDataModelVersion: config.Value.FdoDataModelVersion,
            progress: progress
        );
    }

    public async Task<int> PendingCommitCount(FwDataProject project, string? projectCode)
    {
        var incomingTask = SendReceiveHelpers.PendingMercurialCommits(
            project: project,
            direction: SendReceiveHelpers.PendingCommitDirection.Incoming,
            projectCode: projectCode,
            baseUrl: config.Value.HgWebUrl,
            auth: new SendReceiveHelpers.SendReceiveAuth(config.Value),
            progress: progress
        );
        var outgoingTask = SendReceiveHelpers.PendingMercurialCommits(
            project: project,
            direction: SendReceiveHelpers.PendingCommitDirection.Outgoing,
            projectCode: projectCode,
            baseUrl: config.Value.HgWebUrl,
            auth: new SendReceiveHelpers.SendReceiveAuth(config.Value),
            progress: progress
        );
        var incoming = await incomingTask;
        var outgoing = await outgoingTask;
        // -1 is used to mean "would pull/push all commits", e.g. if we don't have a local clone
        if (incoming < 0) return incoming;
        if (outgoing < 0) return outgoing;
        return incoming + outgoing;
    }

    public async Task CommitFile(string filePath, string commitMessage)
    {
        await SendReceiveHelpers.CommitFile(filePath, commitMessage, progress);
    }
}
