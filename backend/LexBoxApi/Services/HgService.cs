using System.Diagnostics;
using System.IO.Compression;
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

public partial class HgService : IHgService
{
    private const string DELETED_REPO_FOLDER = "_____deleted_____";

    private readonly IOptions<HgConfig> _options;
    private readonly Lazy<HttpClient> _hgClient;
    private readonly ILogger<HgService> _logger;

    public HgService(IOptions<HgConfig> options, IHttpClientFactory clientFactory, ILogger<HgService> logger)
    {
        _options = options;
        _logger = logger;
        _hgClient = new(() => clientFactory.CreateClient("HgWeb"));
    }

    [GeneratedRegex(Project.ProjectCodeRegex)]
    private static partial Regex ProjectCodeRegex();

    public static string PrefixRepoRequestPath(string code) => $"{code[0]}/{code}";
    private string PrefixRepoFilePath(string code) => Path.Combine(_options.Value.RepoPath, code[0].ToString(), code);

    private async Task<HttpResponseMessage> GetResponseMessage(string code, string requestPath)
    {
        if (!ProjectCodeRegex().IsMatch(code))
            throw new ArgumentException($"Invalid project code: {code}.");
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
    public async Task InitRepo(string code)
    {
        AssertIsSafeRepoName(code);
        if (Directory.Exists(PrefixRepoFilePath(code)))
            throw new AlreadyExistsException($"Repo already exists: {code}.");
        await Task.Run(() => InitRepoAt(code));
    }

    private void InitRepoAt(string code)
    {
        var repoDirectory = new DirectoryInfo(PrefixRepoFilePath(code));
        repoDirectory.Create();
        FileUtils.CopyFilesRecursively(
            new DirectoryInfo("Services/HgEmptyRepo"),
            repoDirectory,
            Permissions
        );
    }

    public async Task PrepareEmptyRepo(string code, string tempRepoSuffix)
    {
        var tempRepoName = $"{code}__{tempRepoSuffix}";
        await Task.Run(() =>
        {
            var deletedRepoPath = Path.Combine(_options.Value.RepoPath, DELETED_REPO_FOLDER);
            var directory = Directory.CreateDirectory(deletedRepoPath);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                directory.UnixFileMode = Permissions;
            var dest = Directory.CreateDirectory(Path.Combine(directory.FullName, tempRepoName));
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                dest.UnixFileMode = Permissions;
            FileUtils.CopyFilesRecursively(
                new DirectoryInfo("Services/HgEmptyRepo"),
                dest,
                Permissions
            );
        });
    }

    public async Task MoveEmptyRepoIntoPlace(string code, string tempRepoSuffix)
    {
        var tempRepoName = $"{code}__{tempRepoSuffix}";
        await Task.Run(() =>
        {
            var deletedRepoPath = Path.Combine(_options.Value.RepoPath, DELETED_REPO_FOLDER);
            Directory.Move(
                Path.Combine(deletedRepoPath, tempRepoName),
                PrefixRepoFilePath(code)
            );
        });
    }

    public async Task DeleteRepo(string code)
    {
        await Task.Run(() => Directory.Delete(PrefixRepoFilePath(code), true));
    }

    public BackupExecutor? BackupRepo(string code)
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

    public async Task ResetRepo(string code)
    {
        string timestamp = FileUtils.ToTimestamp(DateTimeOffset.UtcNow);
        await PrepareEmptyRepo(code, $"{timestamp}__empty");
        await SoftDeleteRepo(code, $"{timestamp}__reset");
        //we must init the repo as uploading a zip is optional
        await MoveEmptyRepoIntoPlace(code, $"{timestamp}__empty");
    }

    public async Task FinishReset(string code, Stream zipFile)
    {
        string timestamp = FileUtils.ToTimestamp(DateTimeOffset.UtcNow);
        var tempRepoName = $"{code}__${timestamp}__upload";
        var tempRepoPath = Path.Combine(_options.Value.RepoPath, DELETED_REPO_FOLDER, tempRepoName);
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
                //not sure if this is the best way to handle this, might need to catch it further up to expose the error properly to tus
                throw ProjectResetException.ZipMissingHgFolder();
            }
            //found the .hg folder, move it to the correct location and continue
            Directory.Move(hgFolder, hgPath);
        }
        await CleanupRepoFolder(tempRepoPath);
        SetPermissionsRecursively(tempRepo);
        // Now we're ready to move the new repo into place, replacing the old one
        var realRepoPath = PrefixRepoFilePath(code);
        await DeleteRepo(code); // Do this as late as possible
        Directory.Move(tempRepoPath, realRepoPath);
    }

    /// <summary>
    /// deletes all files and folders in the repo folder except for .hg
    /// </summary>
    private async Task CleanupRepoFolder(string path)
    {
        var repoDir = new DirectoryInfo(path);
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


    public Task RevertRepo(string code, string revHash)
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

    public async Task SoftDeleteRepo(string code, string deletedRepoSuffix)
    {
        var deletedRepoName = $"{code}__{deletedRepoSuffix}";
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

    public bool HasAbandonedTransactions(string projectCode)
    {
        return Path.Exists(Path.Combine(PrefixRepoFilePath(projectCode), ".hg", "store", "journal"));
    }

    public bool RepoIsLocked(string projectCode)
    {
        return Path.Exists(Path.Combine(PrefixRepoFilePath(projectCode), ".hg", "store", "lock"));
    }

    public async Task<string?> GetRepositoryIdentifier(Project project)
    {
        var json = await GetCommit(project.Code, "0");
        return json?["entries"]?.AsArray().FirstOrDefault()?["node"].Deserialize<string>();
    }

    public async Task<DateTimeOffset?> GetLastCommitTimeFromHg(string projectCode)
    {
        var json = await GetCommit(projectCode, "tip");
        //format is this: [1678687688, offset] offset is
        var dateArray = json?["entries"]?[0]?["date"].Deserialize<decimal[]>();
        if (dateArray is null || dateArray.Length != 2 || dateArray[0] <= 0)
            return null;
        //offsets are weird. The format we get the offset in is opposite of how we typically represent offsets, eg normally the US has negative
        //offsets because it's behind UTC. But in other cases the US has positive offsets because time needs to be added to reach UTC.
        //the offset we get here is the latter but dotnet expects the former so we need to invert it.
        var offset = (double)dateArray[1] * -1;
        var date = DateTimeOffset.FromUnixTimeSeconds((long)dateArray[0]).ToOffset(TimeSpan.FromSeconds(offset));
        return date.ToUniversalTime();
    }

    private async Task<JsonObject?> GetCommit(string projectCode, string rev)
    {
        var response = await GetResponseMessage(projectCode, $"log?style=json-lex&rev={rev}");
        return await response.Content.ReadFromJsonAsync<JsonObject>();
    }

    public async Task<Changeset[]> GetChangesets(string projectCode)
    {
        var response = await GetResponseMessage(projectCode, "log?style=json-lex");
        var logResponse = await response.Content.ReadFromJsonAsync<LogResponse>();
        return logResponse?.Changesets ?? Array.Empty<Changeset>();
    }


    public Task<HttpContent> VerifyRepo(string code, CancellationToken token)
    {
        return ExecuteHgCommandServerCommand(code, "verify", token);
    }
    public async Task<HttpContent> ExecuteHgRecover(string code, CancellationToken token)
    {
        var response = await ExecuteHgCommandServerCommand(code, "recover", token);
        // Can't do this with a streamed response, unfortunately. Will have to do it client-side.
        // if (string.IsNullOrWhiteSpace(response)) return "Nothing to recover";
        return response;
    }

    public async Task<int?> GetLexEntryCount(string code)
    {
        var content = await ExecuteHgCommandServerCommand(code, "lexentrycount", default);
        var str = await content.ReadAsStringAsync();
        return int.TryParse(str, out int result) ? result : null;
    }

    private async Task<HttpContent> ExecuteHgCommandServerCommand(string code, string command, CancellationToken token)
    {
        var httpClient = _hgClient.Value;
        var baseUri = _options.Value.HgCommandServer;
        var response = await httpClient.GetAsync($"{baseUri}{code}/{command}", HttpCompletionOption.ResponseHeadersRead, token);
        response.EnsureSuccessStatusCode();
        return response.Content;
    }

    private static readonly string[] InvalidRepoNames = { DELETED_REPO_FOLDER, "api" };

    private void AssertIsSafeRepoName(string name)
    {
        if (InvalidRepoNames.Contains(name, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException($"Invalid repo name: {name}.");
        if (!ProjectCodeRegex().IsMatch(name))
            throw new ArgumentException($"Invalid repo name: {name}.");
    }

    public async Task<ProjectType> DetermineProjectType(string projectCode)
    {
        var response = await GetResponseMessage(projectCode, "file/tip?style=json-lex");
        var parsed = await response.Content.ReadFromJsonAsync<BrowseResponse>();
        bool hasDotSettingsFolder = false;
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
                //OurWord projects have a .Settings folder, but that might not be super reliable
                //so we only use it as a last resort if we didn't match any other project type.
                if (name.Equals(".Settings", StringComparison.OrdinalIgnoreCase))
                {
                    hasDotSettingsFolder = true;
                }
            }
        }

        //if we didn't find any of the above files, check for a .Settings folder
        if (hasDotSettingsFolder) return ProjectType.OurWord;
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
}
