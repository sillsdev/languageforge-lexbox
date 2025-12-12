using System.Text.Json;
using Microsoft.Extensions.Options;

namespace FwHeadless.Services;

public class ProjectMetadataService(IOptions<FwHeadlessConfig> config, ILogger<ProjectMetadataService> logger)
{
    private readonly MetadataStore _store = new(config.Value, logger);

    public Task<bool> IsBlockedFromSyncAsync(Guid projectId)
    {
        return _store.ReadAsync(projectId, metadata => metadata.SyncBlocked?.IsBlocked ?? false);
    }

    public async Task BlockFromSyncAsync(Guid projectId, string? reason = null)
    {
        await _store.UpdateAsync(projectId, metadata =>
        {
            metadata.SyncBlocked = new SyncBlockInfo
            {
                IsBlocked = true,
                Reason = reason ?? "Manual block",
                BlockedAt = DateTime.UtcNow
            };
        });
        logger.LogWarning("Project {projectId} blocked from sync. Reason: {reason}", projectId, reason);
    }

    public async Task UnblockFromSyncAsync(Guid projectId)
    {
        await _store.UpdateAsync(projectId, metadata =>
        {
            metadata.SyncBlocked = new SyncBlockInfo
            {
                IsBlocked = false,
                BlockedAt = null,
                Reason = null
            };
        });
        logger.LogInformation("Project {projectId} unblocked from sync", projectId);
    }

    public Task<SyncBlockInfo?> GetSyncBlockInfoAsync(Guid projectId)
    {
        return _store.ReadAsync(projectId, metadata => metadata.SyncBlocked);
    }

    /// <summary>
    /// Internal metadata store that handles file I/O with locking.
    /// </summary>
    private sealed class MetadataStore(FwHeadlessConfig config, ILogger<ProjectMetadataService> logger)
    {
        private readonly Lock _fileLock = new();

        public Task<T> ReadAsync<T>(Guid projectId, Func<ProjectMetadata, T> action)
        {
            return Task.Run(() =>
            {
                try
                {
                    lock (_fileLock)
                    {
                        var metadata = LoadMetadata(projectId);
                        return action(metadata);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error reading metadata for project {projectId}", projectId);
                    throw;
                }
            });
        }

        public Task UpdateAsync(Guid projectId, Action<ProjectMetadata> action)
        {
            return Task.Run(() =>
            {
                try
                {
                    lock (_fileLock)
                    {
                        var metadata = LoadMetadata(projectId);
                        action(metadata);
                        SaveMetadata(projectId, metadata);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error updating metadata for project {projectId}", projectId);
                    throw;
                }
            });
        }

        private ProjectMetadata LoadMetadata(Guid projectId)
        {
            var metadataPath = GetMetadataPath(projectId);
            if (File.Exists(metadataPath))
            {
                try
                {
                    var json = File.ReadAllText(metadataPath);
                    return JsonSerializer.Deserialize<ProjectMetadata>(json) ?? new ProjectMetadata();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error parsing metadata file for project {projectId}", projectId);
                }
            }
            return new ProjectMetadata();
        }

        private void SaveMetadata(Guid projectId, ProjectMetadata metadata)
        {
            var metadataPath = GetMetadataPath(projectId);
            var dirPath = Path.GetDirectoryName(metadataPath);
            if (dirPath != null && !Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(metadata, options);
            File.WriteAllText(metadataPath, json);
        }

        private string GetMetadataPath(Guid projectId)
        {
            var projectFolder = config.GetProjectFolder(projectId);
            return Path.Combine(projectFolder, "metadata.json");
        }
    }
}

public class ProjectMetadata
{
    public SyncBlockInfo? SyncBlocked { get; set; }
}

public class SyncBlockInfo
{
    public bool IsBlocked { get; set; }
    public string? Reason { get; set; }
    public DateTime? BlockedAt { get; set; }
}
