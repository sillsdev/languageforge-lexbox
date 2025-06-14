using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LexBoxApi.Otel;
using LexCore.Config;
using LexCore.Entities;
using LexCore.Exceptions;
using LexCore.ServiceInterfaces;
using LexCore.Utils;
using LexSyncReverseProxy;
using Microsoft.Extensions.Options;
using Path = System.IO.Path;

namespace LexBoxApi.Services;

public partial class HgService : IHgService, IHostedService
{
    private const string DELETED_REPO_FOLDER = ProjectCode.DELETED_REPO_FOLDER;
    private const string TEMP_REPO_FOLDER = ProjectCode.TEMP_REPO_FOLDER;

    private const string AllZeroHash = "0000000000000000000000000000000000000000";

    private readonly IOptions<HgConfig> _options;
    private readonly Lazy<HttpClient> _hgClient;
    private readonly ILogger<HgService> _logger;

    public HgService(IOptions<HgConfig> options, IHttpClientFactory clientFactory, ILogger<HgService> logger)
    {
        _options = options;
        _logger = logger;
        _hgClient = new(() => clientFactory.CreateClient("HgWeb"));
    }

    public static string PrefixRepoRequestPath(ProjectCode code) => $"{code.Value[0]}/{code}";
    private string PrefixRepoFilePath(ProjectCode code) => Path.Combine(_options.Value.RepoPath, code.Value[0].ToString(), code.Value);
    private string GetTempRepoPath(ProjectCode code, string reason) => Path.Combine(_options.Value.RepoPath, TEMP_REPO_FOLDER, $"{code}__{reason}__{FileUtils.ToTimestamp(DateTimeOffset.UtcNow)}");

    private async Task<HttpResponseMessage> GetResponseMessage(ProjectCode code, string requestPath)
    {
        var client = _hgClient.Value;

        var urlPrefix = DetermineProjectUrlPrefix(HgType.hgWeb, _options.Value);
        var requestUri = $"{urlPrefix}{PrefixRepoRequestPath(code)}/{requestPath}";
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUri));
        response.EnsureSuccessStatusCode();
        return response;
    }

    /// <summary>
    /// Note: The repo is unstable and potentially unavailable for a short while after creation, so don't read from it right away.
    /// See: https://github.com/sillsdev/languageforge-lexbox/issues/173#issuecomment-1665478630
    /// </summary>
    public async Task InitRepo(ProjectCode code)
    {
        if (Directory.Exists(PrefixRepoFilePath(code)))
            throw new AlreadyExistsException($"Repo already exists: {code}.");
        await Task.Run(() =>
        {
            InitRepoAt(new DirectoryInfo(PrefixRepoFilePath(code)));
        });
        await InvalidateDirCache(code);
        await WaitForRepoEmptyState(code, RepoEmptyState.Empty);
    }

    private void InitRepoAt(DirectoryInfo repoDirectory)
    {
        repoDirectory.Create();
        FileUtils.CopyFilesRecursively(
            new DirectoryInfo("Services/HgEmptyRepo"),
            repoDirectory,
            Permissions
        );
    }

    /// <summary>
    /// danger, this will replace the repo at the destination with the source
    /// </summary>
    public async Task CopyRepo(ProjectCode sourceCode, ProjectCode destCode)
    {
        var sourceFolder = new DirectoryInfo(PrefixRepoFilePath(sourceCode));
        if (!sourceFolder.Exists)
        {
            throw new DirectoryNotFoundException($"Source repo {sourceCode} not found");
        }
        var repoDirectory = new DirectoryInfo(PrefixRepoFilePath(destCode));
        if (repoDirectory.Exists && (await GetTipHash(destCode) != AllZeroHash))
        {
            throw new InvalidOperationException($"Destination repo {destCode} already exists and is not empty");
        }

        await Task.Run(() =>
            {
                if (repoDirectory.Exists) repoDirectory.Delete(true);
                repoDirectory.Create();
                FileUtils.CopyFilesRecursively(
                    sourceFolder,
                    repoDirectory,
                    Permissions
                );
            }
        );
        await InvalidateDirCache(destCode);
        await WaitForRepoReady(destCode);
    }

    public async Task DeleteRepo(ProjectCode code)
    {
        await Task.Run(() => Directory.Delete(PrefixRepoFilePath(code), true));
    }

    public BackupExecutor? BackupRepo(ProjectCode code)
    {
        string repoPath = PrefixRepoFilePath(code);
        if (!Directory.Exists(repoPath))
        {
            return null;
        }
        return new((stream, token) => Task.Run(() =>
        {
            ZipFile.CreateFromDirectory(repoPath, stream, CompressionLevel.Fastest, false);
        }, token));
    }

    public async Task ResetRepo(ProjectCode code)
    {
        var tmpRepo = new DirectoryInfo(GetTempRepoPath(code, "reset"));
        InitRepoAt(tmpRepo);
        await SoftDeleteRepo(code, ResetSoftDeleteSuffix(DateTimeOffset.UtcNow));
        //we must init the repo as uploading a zip is optional
        tmpRepo.MoveTo(PrefixRepoFilePath(code));
        await InvalidateDirCache(code);
        await WaitForRepoEmptyState(code, RepoEmptyState.Empty);
    }

    public static string ResetSoftDeleteSuffix(DateTimeOffset resetAt)
    {
        return $"{FileUtils.ToTimestamp(resetAt)}__reset";
    }

    public async Task FinishReset(ProjectCode code, Stream zipFile)
    {
        var tempRepoPath = GetTempRepoPath(code, "upload");
        var tempRepo = Directory.CreateDirectory(tempRepoPath);
        // TODO: Is Task.Run superfluous here? Or a good idea? Don't know the ins and outs of what happens before the first await in an async method in ASP.NET Core...
        await Task.Run(() =>
        {
            using var archive = new ZipArchive(zipFile, ZipArchiveMode.Read);
            archive.ExtractToDirectory(tempRepoPath);
        });

        var hgPath = Path.Join(tempRepoPath, ".hg");
        if (!Directory.Exists(hgPath))
        {
            var hgFolder = Directory.EnumerateDirectories(tempRepoPath, ".hg", SearchOption.AllDirectories)
                .FirstOrDefault();
            if (hgFolder is null)
            {
                // Don't want to leave invalid .zip contents lying around as they may have been quite large
                Directory.Delete(tempRepoPath, true);
                //not sure if this is the best way to handle this, might need to catch it further up to expose the error properly to tus
                throw ProjectResetException.ZipMissingHgFolder();
            }
            //found the .hg folder, move it to the correct location and continue
            Directory.Move(hgFolder, hgPath);
        }
        await CleanupRepoFolder(tempRepo);
        SetPermissionsRecursively(tempRepo);
        // Now we're ready to move the new repo into place, replacing the old one
        await DeleteRepo(code);
        tempRepo.MoveTo(PrefixRepoFilePath(code));
        await InvalidateDirCache(code);
        await WaitForRepoReady(code);
    }

    public async Task<string[]> CleanupResetBackups(bool dryRun = false)
    {
        using var activity = LexBoxActivitySource.Get().StartActivity();
        List<string> deletedRepos = [];
        int deletedCount = 0;
        int totalCount = 0;
        foreach (var deletedRepo in Directory.EnumerateDirectories(Path.Combine(_options.Value.RepoPath, DELETED_REPO_FOLDER)))
        {
            totalCount++;
            var deletedRepoName = Path.GetFileName(deletedRepo);
            var resetDate = GetResetDate(deletedRepoName);
            if (resetDate is null)
            {
                continue;
            }
            var resetAge = DateTimeOffset.UtcNow - resetDate.Value;
            //enforce a minimum age threshold, just in case the threshold is set too low
            var ageThreshold = TimeSpan.FromDays(Math.Max(_options.Value.ResetCleanupAgeDays, 5));
            if (resetAge <= ageThreshold) continue;
            try
            {
                if (!dryRun)
                    await Task.Run(() => Directory.Delete(deletedRepo, true));
                deletedRepos.Add(deletedRepoName);
                deletedCount++;
            }
            catch (Exception e)
            {
                activity?.AddTag("app.hg.cleanupresetbackups.error", e.Message);
            }
        }
        activity?.AddTag("app.hg.cleanupresetbackups", totalCount);
        activity?.AddTag("app.hg.cleanupresetbackups.deleted", deletedCount);
        activity?.AddTag("app.hg.cleanupresetbackups.dryrun", dryRun);
        return deletedRepos.ToArray();
    }

    public static DateTimeOffset? GetResetDate(string repoName)
    {
        var match = ResetProjectsRegex().Match(repoName);
        if (!match.Success) return null;
        return FileUtils.ToDateTimeOffset(match.Groups[1].Value);
    }

    [GeneratedRegex(@"__(\d{4}-\d{2}-\d{2}T\d{2}-\d{2}-\d{2})__reset$")]
    public static partial Regex ResetProjectsRegex();


    /// <summary>
    /// deletes all files and folders in the repo folder except for .hg
    /// </summary>
    private async Task CleanupRepoFolder(DirectoryInfo repoDir)
    {
        await Task.Run(() =>
        {
            foreach (var info in repoDir.EnumerateFileSystemInfos())
            {
                if (info.Name == ".hg") continue;
                if (info is DirectoryInfo dir) dir.Delete(true);
                else info.Delete();
            }
        });
    }

    /// <summary>
    /// Returns either an empty string, or XML (in string form) with a root LangTags element containing five child elements: AnalysisWss, CurAnalysisWss, VernWss, CurVernWss, and CurPronunWss.
    /// Each child element will contain a single `<Uni>` element whose text content is a list of tags separated by spaces.
    /// </summary>
    private async Task<string> GetLangTagsAsXml(ProjectCode code, CancellationToken token = default)
    {
        var result = await ExecuteHgCommandServerCommand(code, "flexwritingsystems", token);
        var xmlBody = await result.ReadAsStringAsync(token);
        if (string.IsNullOrEmpty(xmlBody)) return string.Empty;
        return $"<LangTags>{xmlBody}</LangTags>";
    }

    private string[] GetWsList(System.Xml.XmlElement root, string tagName)
    {
        var wsStr = root[tagName]?["Uni"]?.InnerText ?? "";
        // String.Split(null) splits on any whitespace, but needs a type cast so the compiler can tell which overload (char[] vs string[]) to use
        return wsStr.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
    }

    public async Task<ProjectWritingSystems?> GetProjectWritingSystems(ProjectCode code, CancellationToken token = default)
    {
        var langTagsXml = await GetLangTagsAsXml(code, token);
        if (string.IsNullOrEmpty(langTagsXml)) return null;
        var doc = new System.Xml.XmlDocument();
        doc.LoadXml(langTagsXml);
        var root = doc.DocumentElement;
        if (root is null) return null;
        var vernWss = GetWsList(root, "VernWss");
        var analysisWss = GetWsList(root, "AnalysisWss");
        var curVernWss = GetWsList(root, "CurVernWss");
        var curAnalysisWss = GetWsList(root, "CurAnalysisWss");
        var curVernSet = curVernWss.ToHashSet();
        var curAnalysisSet = curAnalysisWss.ToHashSet();
        // Ordering is important here to match how FLEx handles things: all *current* writing systems first, then all *non-current*.
        var vernWsIds = curVernWss.Select((tag, idx) => new FLExWsId { Tag = tag, IsActive = true, IsDefault = idx == 0 }).ToList();
        var analysisWsIds = curAnalysisWss.Select((tag, idx) => new FLExWsId { Tag = tag, IsActive = true, IsDefault = idx == 0 }).ToList();
        vernWsIds.AddRange(vernWss.Where(ws => !curVernSet.Contains(ws)).Select(tag => new FLExWsId { Tag = tag, IsActive = false, IsDefault = false }));
        analysisWsIds.AddRange(analysisWss.Where(ws => !curAnalysisSet.Contains(ws)).Select(tag => new FLExWsId { Tag = tag, IsActive = false, IsDefault = false }));
        return new ProjectWritingSystems
        {
            VernacularWss = vernWsIds,
            AnalysisWss = analysisWsIds
        };
    }

    public async Task<Guid?> GetProjectIdOfFlexProject(ProjectCode code, CancellationToken token = default)
    {
        var result = await ExecuteHgCommandServerCommand(code, "flexprojectid", token);
        var text = await result.ReadAsStringAsync(token);
        if (Guid.TryParse(text, out var guid)) return guid;
        return null;
    }

    public async Task<int?> GetModelVersionOfFlexProject(ProjectCode code, CancellationToken token = default)
    {
        var result = await ExecuteHgCommandServerCommand(code, "flexmodelversion", token);
        var text = await result.ReadAsStringAsync(token);
        if (string.IsNullOrWhiteSpace(text)) return null;
        try
        {
            var json = JsonDocument.Parse(text);
            if (json.RootElement.TryGetProperty("modelversion", out var modelversion) && modelversion.TryGetInt32(out int version)) return version;
            _logger.LogError("Invalid JSON {text} in GetModelVersionOfFlexProject, should have one property \"modelversion\" that's a number", text);
            return null;
        }
        catch (JsonException e)
        {
            _logger.LogError("Malformed JSON {text} in GetModelVersionOfFlexProject: {error}", text, e.ToString());
            return null;
        }
    }

    public Task RevertRepo(ProjectCode code, string revHash)
    {
        throw new NotImplementedException();
        // Steps:
        // 1. Rename repo to repo-backup-date (verifying first that it does not exist, adding -NNN at the end (001, 002, 003) if it does)
        // 2. Make empty directory (NOT a repo yet) with this project code
        // 3. Clone repo-backup-date-NNN into empty directory, passing "-r revHash" param
        // 4. Copy .hg/hgrc from backup dir, overwriting the one in the cloned dir
        //
        // Will need an SSH key as a k8s secret, put it into authorized_keys on the hgweb side so that lexbox can do "ssh hgweb hg clone ..."
    }

    public async Task SoftDeleteRepo(ProjectCode code, string deletedRepoSuffix)
    {
        var deletedRepoName = DeletedRepoName(code, deletedRepoSuffix);
        await Task.Run(() =>
        {
            var deletedRepoPath = Path.Combine(_options.Value.RepoPath, DELETED_REPO_FOLDER);
            var directory = Directory.CreateDirectory(deletedRepoPath);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                directory.UnixFileMode = Permissions;
            Directory.Move(
                PrefixRepoFilePath(code),
                Path.Combine(deletedRepoPath, deletedRepoName));
        });
    }

    public static string DeletedRepoName(ProjectCode code, string deletedRepoSuffix)
    {
        return $"{code}__{deletedRepoSuffix}";
    }

    private const UnixFileMode Permissions = UnixFileMode.GroupRead | UnixFileMode.GroupWrite |
                                             UnixFileMode.GroupExecute | UnixFileMode.SetGroup |
                                             UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                                             UnixFileMode.SetUser;

    private static void SetPermissionsRecursively(DirectoryInfo rootDir)
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return;
        rootDir.UnixFileMode = Permissions;

        foreach (var dir in rootDir.EnumerateDirectories())
        {
            dir.UnixFileMode = Permissions;
            SetPermissionsRecursively(dir);
        }

        foreach (var file in rootDir.EnumerateFiles())
        {
            file.UnixFileMode = Permissions;
        }
    }

    public bool HasAbandonedTransactions(ProjectCode projectCode)
    {
        return Path.Exists(Path.Combine(PrefixRepoFilePath(projectCode), ".hg", "store", "journal"));
    }

    public bool RepoIsLocked(ProjectCode projectCode)
    {
        return Path.Exists(Path.Combine(PrefixRepoFilePath(projectCode), ".hg", "store", "lock"));
    }

    public async Task<string?> GetRepositoryIdentifier(Project project)
    {
        var json = await GetCommit(project.Code, "0");
        return json?["entries"]?.AsArray().FirstOrDefault()?["node"].Deserialize<string>();
    }

    public async Task<DateTimeOffset?> GetLastCommitTimeFromHg(ProjectCode projectCode)
    {
        var dateStr = await GetTipDate(projectCode);
        return ConvertHgDate(dateStr);
    }

    private async Task<JsonObject?> GetCommit(ProjectCode projectCode, string rev)
    {
        var response = await GetResponseMessage(projectCode, $"log?style=json-lex&rev={rev}");
        return await response.Content.ReadFromJsonAsync<JsonObject>();
    }

    public async Task<Changeset[]> GetChangesets(ProjectCode projectCode)
    {
        var response = await GetResponseMessage(projectCode, "log?style=json-lex");
        var logResponse = await response.Content.ReadFromJsonAsync<LogResponse>();
        return logResponse?.Changesets ?? Array.Empty<Changeset>();
    }

    public Task<HttpContent> VerifyRepo(ProjectCode code, CancellationToken token)
    {
        return ExecuteHgCommandServerCommand(code, "verify", token);
    }

    public async Task<HttpContent> ExecuteHgRecover(ProjectCode code, CancellationToken token)
    {
        var response = await ExecuteHgCommandServerCommand(code, "recover", token);
        // Can't do this with a streamed response, unfortunately. Will have to do it client-side.
        // if (string.IsNullOrWhiteSpace(response)) return "Nothing to recover";
        return response;
    }

    public Task<HttpContent> InvalidateDirCache(ProjectCode code, CancellationToken token = default)
    {
        var repoPath = Path.Join(PrefixRepoFilePath(code));
        if (Directory.Exists(repoPath))
        {
            // Invalidate NFS directory cache by forcing a write and re-read of the repo directory
            var randomPath = Path.Join(repoPath, Path.GetRandomFileName());
            while (File.Exists(randomPath) || Directory.Exists(randomPath)) { randomPath = Path.Join(repoPath, Path.GetRandomFileName()); }
            try
            {
                // Create and delete a directory since that's slightly safer than a file
                var d = Directory.CreateDirectory(randomPath);
                d.Delete();
            }
            catch (Exception) { }
        }
        var result = ExecuteHgCommandServerCommand(code, "invalidatedircache", token);
        return result;
    }

    public static DateTimeOffset? ConvertHgDate(string? dateStr)
    {
        // Format is "1472445535 -25200", two ints separated by a single space.
        // "0 0" is returned for empty repos (no commits), but we prefer to represent that as null
        if (dateStr == "0 0") return null;
        var dateArray = dateStr?.Split();
        if (dateArray is null || dateArray.Length != 2) return null;
        // Parse to 64-bit ints so we don't have a year 2038 problem
        if (!long.TryParse(dateArray[0], out var timestamp)) return null;
        if (!long.TryParse(dateArray[1], out var offset)) return null;
        //hg offsets are weird. The format we get the offset in is opposite of how we typically represent offsets, eg normally the US has negative
        //offsets because it's behind UTC. But in other cases the US has positive offsets because time needs to be added to reach UTC.
        //the offset we get here is the latter but dotnet expects the former so we need to invert it.
        var date = DateTimeOffset.FromUnixTimeSeconds(timestamp).ToOffset(TimeSpan.FromSeconds(offset * -1));
        return date.ToUniversalTime();
    }

    public async Task<string> GetTipDate(ProjectCode code, CancellationToken token = default)
    {
        var content = await ExecuteHgCommandServerCommand(code, "tipdate", token);
        return await content.ReadAsStringAsync();
    }

    public async Task<string> GetTipHash(ProjectCode code, CancellationToken token = default)
    {
        var content = await ExecuteHgCommandServerCommand(code, "tip", token);
        return await content.ReadAsStringAsync();
    }

    public async Task<int?> GetRepoSizeInKb(ProjectCode code, CancellationToken token = default)
    {
        var content = await ExecuteHgCommandServerCommand(code, "reposizeinkb", token);
        var sizeStr = await content.ReadAsStringAsync();
        return int.TryParse(sizeStr, out var size) ? size : null;
    }

    private async Task WaitForRepoReady(ProjectCode code)
    {
        // If someone uploaded an *empty* repo, we don't want to wait forever for a non-empty state
        var changelogPath = Path.Join(PrefixRepoFilePath(code), ".hg", "store", "00changelog.i");
        var expectedState = File.Exists(changelogPath) ? RepoEmptyState.NonEmpty : RepoEmptyState.Empty;
        await WaitForRepoEmptyState(code, expectedState);
    }

    private async Task WaitForRepoEmptyState(ProjectCode code, RepoEmptyState expectedState, int timeoutMs = 30_000, CancellationToken token = default)
    {
        // Set timeout so unforeseen errors can't cause an infinite loop
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeoutSource.CancelAfter(timeoutMs);
        var done = false;
        try
        {
            while (!done && !timeoutSource.IsCancellationRequested)
            {
                var hash = await GetTipHash(code, timeoutSource.Token);
                var isEmpty = hash == AllZeroHash;
                done = expectedState switch
                {
                    RepoEmptyState.Empty => isEmpty,
                    RepoEmptyState.NonEmpty => !isEmpty,
                    _ => isEmpty
                };
                if (!done) await Task.Delay(2500, timeoutSource.Token);
            }
        }
        // We don't want to actually throw if we hit the timeout, because the operation *will* succeed eventually
        // once the NFS caches synchronize, so we don't want to propagate an error message to the end user. So
        // even if the timeout is hit, return as if we succeeded.
        catch (OperationCanceledException) { }
    }

    public async Task<int?> GetLexEntryCount(ProjectCode code, ProjectType projectType)
    {
        var command = projectType switch
        {
            ProjectType.FLEx => "lexentrycount",
            ProjectType.WeSay => "wesaylexentrycount",
            _ => null
        };
        if (command is null) return null;
        var content = await ExecuteHgCommandServerCommand(code, command, default);
        var str = await content.ReadAsStringAsync();
        return int.TryParse(str, out int result) ? result : null;
    }

    public async Task<string> HgCommandHealth()
    {
        var content = await ExecuteHgCommandServerCommand("health", "healthz", default);
        var version = await content.ReadAsStringAsync();
        return version.Trim();
    }

    public async Task<ZipArchive?> GetLdmlZip(ProjectCode code, CancellationToken token = default)
    {
        var content = await MaybeExecuteHgCommandServerCommand(code, "ldmlzip", [HttpStatusCode.Forbidden], token);
        if (content is null) return null;
        return new ZipArchive(await content.ReadAsStreamAsync(token), ZipArchiveMode.Read);
    }

    private async Task<HttpContent> ExecuteHgCommandServerCommand(ProjectCode code, string command, CancellationToken token)
    {
        var httpClient = _hgClient.Value;
        var baseUri = _options.Value.HgCommandServer;
        var response = await httpClient.GetAsync($"{baseUri}{code}/{command}", HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        return response.Content;
    }

    private async Task<HttpContent?> MaybeExecuteHgCommandServerCommand(ProjectCode code, string command, IEnumerable<HttpStatusCode> okErrors, CancellationToken token)
    {
        var httpClient = _hgClient.Value;
        var baseUri = _options.Value.HgCommandServer;
        var response = await httpClient.GetAsync($"{baseUri}{code}/{command}", HttpCompletionOption.ResponseHeadersRead, token);
        if (okErrors.Contains(response.StatusCode)) return null;
        response.EnsureSuccessStatusCode();
        return response.Content;
    }

    public async Task<ProjectType> DetermineProjectType(ProjectCode projectCode)
    {
        var response = await GetResponseMessage(projectCode, "file/tip?style=json-lex");
        var parsed = await response.Content.ReadFromJsonAsync<BrowseResponse>();
        // TODO: Move the heuristics below to a ProjectHeuristics class?
        foreach (var file in parsed?.Files ?? [])
        {
            if (file.Basename is { } name)
            {
                const string flexFilename = "FLExProject.ModelVersion";
                if (name.Equals(flexFilename, StringComparison.Ordinal))
                {
                    return ProjectType.FLEx;
                }
                const string oseFilename = "OsMetaData.xml";
                if (name.Equals(oseFilename, StringComparison.Ordinal))
                {
                    return ProjectType.OneStoryEditor;
                }
                string oseProjectFilename = $"{projectCode}.onestory";
                if (name.Equals(oseProjectFilename, StringComparison.OrdinalIgnoreCase))
                {
                    return ProjectType.OneStoryEditor;
                }
                const string wesaySuffix = ".WeSayConfig";
                if (name.EndsWith(wesaySuffix, StringComparison.Ordinal))
                {
                    return ProjectType.WeSay;
                }
                const string adaptItConfigFile = "AI-ProjectConfiguration.aic";
                if (name.Equals(adaptItConfigFile, StringComparison.Ordinal))
                {
                    return ProjectType.AdaptIt;
                }
            }
        }

        //if we didn't find any of the above files, check for a .Settings folder
        //OurWord projects have a .Settings folder, but that might not be super reliable
        //so we only use it as a last resort if we didn't match any other project type.
        foreach (var dir in parsed?.Directories ?? [])
        {
            if (dir.Basename is { } name)
            {
                if (name.Equals(".Settings", StringComparison.OrdinalIgnoreCase))
                {
                    return ProjectType.OurWord;
                }
            }
        }
        return ProjectType.Unknown;
    }

    /// <summary>
    /// returns the url prefix for the given project code and migration status
    /// will end with a /
    /// </summary>
    public static string DetermineProjectUrlPrefix(HgType type, HgConfig hgConfig)
    {
        return (type) switch
        {
            (HgType.hgWeb) => hgConfig.HgWebUrl,
            (HgType.resumable) => hgConfig.HgResumableUrl,
            _ => throw new ArgumentException(
                $"Unknown request, HG request type: {type}")
        };
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var repoContainerDirectories = ProjectCode.SpecialDirectoryNames
            .Concat(Enumerable.Range('a', 'z' - 'a' + 1).Select(c => ((char)c).ToString()))
            .Concat(Enumerable.Range(0, 10).Select(c => c.ToString()));

        foreach (var directory in repoContainerDirectories)
        {
            var path = Path.Combine(_options.Value.RepoPath, directory);
            var dirInfo = Directory.CreateDirectory(path);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                dirInfo.UnixFileMode = Permissions;
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public class LogResponse
{
    public string? Node { get; set; }
    public int? ChangesetCount { get; set; }
    public Changeset[]? Changesets { get; set; }
}

public class BrowseFilesResponse
{
    public string? Abspath { get; set; }
    public string? Basename { get; set; }
}

public class BrowseResponse
{
    public BrowseFilesResponse[]? Files { get; set; }
    public BrowseFilesResponse[]? Directories { get; set; }
}

public enum RepoEmptyState
{
    Empty,
    NonEmpty
}
