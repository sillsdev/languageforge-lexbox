using LcmCrdt.MediaServer;
using Microsoft.Extensions.Logging;

namespace FwLiteShared.LocalSync;

/// <summary>
/// Somewhere a device's media files (pictures, audio) are copied to and from during local sync, keyed by resource
/// id: a sync folder, or (later) a local sync host. Only the files move; the resources themselves travel as
/// commits, and their Lexbox upload state is never touched.
/// </summary>
public interface IMediaStore
{
    /// <summary>The resources this store has files for.</summary>
    Task<IReadOnlyCollection<Guid>> ResourceIds();

    /// <summary>Which of the offered resources this store doesn't have yet, so only those are sent.</summary>
    Task<IReadOnlyCollection<Guid>> Wanted(IReadOnlyCollection<Guid> offered);

    Task<StoredMedia?> Get(Guid resourceId);

    /// <summary>Stores a file. Does nothing if the store already has one for this resource.</summary>
    Task Put(Guid resourceId, Stream content, string fileName);
}

public sealed record StoredMedia(Stream Content, string FileName) : IAsyncDisposable
{
    public async ValueTask DisposeAsync() => await Content.DisposeAsync();
}

/// <param name="Failed">Files that couldn't be copied (e.g. unreadable); they are retried on the next sync.</param>
public record MediaCopyCounts(int Copied, int Failed);

/// <summary>
/// Media in a sync folder: <c>media/&lt;resource id&gt;/&lt;original file name&gt;</c>. Files are written under a
/// temporary name and then renamed, so a drive pulled out mid-copy never leaves a half-written file in place.
/// </summary>
public class FolderMediaStore(string syncFolderPath) : IMediaStore
{
    private readonly string _mediaPath = Path.Combine(syncFolderPath, "media");

    public Task<IReadOnlyCollection<Guid>> ResourceIds()
    {
        if (!Directory.Exists(_mediaPath)) return Task.FromResult<IReadOnlyCollection<Guid>>([]);
        IReadOnlyCollection<Guid> ids = [.. Directory.EnumerateDirectories(_mediaPath)
            .Select(dir => Guid.TryParse(Path.GetFileName(dir), out var id) && FileIn(dir) is not null ? id : (Guid?)null)
            .OfType<Guid>()];
        return Task.FromResult(ids);
    }

    public async Task<IReadOnlyCollection<Guid>> Wanted(IReadOnlyCollection<Guid> offered)
    {
        var held = (await ResourceIds()).ToHashSet();
        return [.. offered.Where(id => !held.Contains(id))];
    }

    public Task<StoredMedia?> Get(Guid resourceId)
    {
        var file = FileIn(ResourceDirectory(resourceId));
        return Task.FromResult(file is null ? null : new StoredMedia(File.OpenRead(file), Path.GetFileName(file)));
    }

    public async Task Put(Guid resourceId, Stream content, string fileName)
    {
        var directory = ResourceDirectory(resourceId);
        if (FileIn(directory) is not null) return;
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, MediaFileNames.MakeSafe(Path.GetFileName(fileName)));
        var tempPath = Path.Combine(directory, MediaFileNames.PartialPrefix + Guid.NewGuid().ToString("N"));
        try
        {
            await using (var file = File.Create(tempPath))
            {
                await content.CopyToAsync(file);
            }
            File.Move(tempPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private string ResourceDirectory(Guid resourceId) => Path.Combine(_mediaPath, resourceId.ToString("N"));

    /// <summary>The finished file for a resource; a leftover partial file from an interrupted copy doesn't count.</summary>
    private static string? FileIn(string directory) =>
        Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory)
                .FirstOrDefault(f => !Path.GetFileName(f).StartsWith(MediaFileNames.PartialPrefix, StringComparison.Ordinal))
            : null;
}

/// <summary>
/// Copies media files between this device and a store.
/// </summary>
public class MediaCopier(LcmMediaService mediaService, ILogger<MediaCopier> logger)
{
    /// <summary>Sends the store every file it lacks. A file that fails is skipped, so it can't block the rest.</summary>
    public async Task<MediaCopyCounts> Push(IMediaStore store)
    {
        var cached = (await mediaService.MediaForLocalSync()).Cached.ToDictionary(r => r.Id);
        var wanted = await store.Wanted([.. cached.Keys]);
        var copied = 0;
        var failed = 0;
        foreach (var resourceId in wanted)
        {
            var resource = cached[resourceId];
            try
            {
                await using var content = File.OpenRead(resource.LocalPath!);
                await store.Put(resourceId, content, resource.Metadata?.Filename ?? Path.GetFileName(resource.LocalPath!));
                copied++;
            }
            catch (Exception e)
            {
                failed++;
                logger.LogWarning(e, "Failed to copy media file {ResourceId} for local sync", resourceId);
            }
        }
        return new MediaCopyCounts(copied, failed);
    }

    /// <summary>
    /// Takes every file this device lacks for resources the project uses. A file that fails is skipped, so it can't
    /// block the rest.
    /// </summary>
    public async Task<MediaCopyCounts> PullAll(IMediaStore store)
    {
        var available = (await store.ResourceIds()).ToHashSet();
        var copied = 0;
        var failed = 0;
        foreach (var resourceId in (await mediaService.MediaForLocalSync()).NotCached.Where(available.Contains))
        {
            try
            {
                await using var media = await store.Get(resourceId);
                if (media is not null && await mediaService.AddLocalCopy(resourceId, media.Content, media.FileName)) copied++;
            }
            catch (Exception e)
            {
                failed++;
                logger.LogWarning(e, "Failed to copy media file {ResourceId} from local sync", resourceId);
            }
        }
        return new MediaCopyCounts(copied, failed);
    }
}
