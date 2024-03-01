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

    private async Task<HttpResponseMessage> GetResponseMessage(ProjectMigrationStatus migrationStatus, string code, string requestPath)
    {
        if (!ProjectCodeRegex().IsMatch(code))
            throw new ArgumentException($"Invalid project code: {code}.");
        var client = _hgClient.Value;
        AuthenticationHeaderValue? authHeader = null;
        if (migrationStatus is ProjectMigrationStatus.PrivateRedmine or ProjectMigrationStatus.PublicRedmine)
        {
            authHeader = new ("Basic", Convert.ToBase64String(Encoding.ASCII.GetBytes($"lexbox:{_options.Value.RedmineTrustToken}")));
        }

        var urlPrefix = DetermineProjectUrlPrefix(HgType.hgWeb, code, migrationStatus, _options.Value);
        var requestUri = $"{urlPrefix}{code}/{requestPath}";
        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, requestUri)
        {
            Headers = { Authorization = authHeader }
        });
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
        if (Directory.Exists(Path.Combine(_options.Value.RepoPath, code)))
            throw new AlreadyExistsException($"Repo already exists: {code}.");
        await Task.Run(() => InitRepoAt(_options.Value.RepoPath, code));
    }

    public static void InitRepoAt(string repoPath, string code)
    {
        CopyFilesRecursively(
            new DirectoryInfo("Services/HgEmptyRepo"),
            new DirectoryInfo(repoPath).CreateSubdirectory(code)
        );
    }

    public async Task DeleteRepo(string code)
    {
        await Task.Run(() => Directory.Delete(Path.Combine(_options.Value.RepoPath, code), true));
    }

    public BackupExecutor? BackupRepo(string code)
    {
        string repoPath = Path.Combine(_options.Value.RepoPath, code);
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
        await SoftDeleteRepo(code, $"{timestamp}__reset");
        //we must init the repo as uploading a zip is optional
        await InitRepo(code);
    }

    public async Task FinishReset(string code, Stream zipFile)
    {
        using var archive = new ZipArchive(zipFile, ZipArchiveMode.Read);
        await DeleteRepo(code);
        var repoPath = Path.Combine(_options.Value.RepoPath, code);
        var dir = Directory.CreateDirectory(repoPath);
        archive.ExtractToDirectory(repoPath);

        var hgPath = Path.Join(repoPath, ".hg");
        if (!Directory.Exists(hgPath))
        {
            var hgFolder = Directory.EnumerateDirectories(repoPath, ".hg", SearchOption.AllDirectories)
                .FirstOrDefault();
            if (hgFolder is null)
            {
                await DeleteRepo(code);
                await InitRepo(code); // we don't want 404s
                //not sure if this is the best way to handle this, might need to catch it further up to expose the error properly to tus
                throw ProjectResetException.ZipMissingHgFolder();
            }
            //found the .hg folder, move it to the correct location and continue
            Directory.Move(hgFolder, hgPath);
        }
        await CleanupRepoFolder(repoPath);
        SetPermissionsRecursively(dir);
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

    public async Task<bool> MigrateRepo(Project project, CancellationToken cancellationToken)
    {
        using var activity = LexBoxActivitySource.Get().StartActivity();
        _logger.LogInformation("Migrating repo {Code}", project.Code);
        activity?.AddTag("app.project_code", project.Code);
        //rsync data from remote server to /hg-repos
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            _logger.LogWarning("Skipping migration of repo {Code} because it's running on Windows," +
                               " in debug builds this will report the migration as successful", project.Code);
#if DEBUG
            return true;
#else
            return false;
#endif
        }
        var repoPath = Path.GetFullPath(_options.Value.RepoPath);
        var remoteHost = _options.Value.MigrationHost;
        var remotePathPart = project.ProjectOrigin == ProjectMigrationStatus.PublicRedmine ? "public" : "private";
        var remotePath = $"/var/vcs/{remotePathPart}/{project.Code}";
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "/bin/sh",
            CreateNoWindow = true,
            RedirectStandardError = true,
            RedirectStandardOutput = false,
            //.hg/cache is excluded since there can be issues reading and it's not required
            Arguments = $"""
                         -c "rsync -axhAXP --del --groupmap=hg:www-data --exclude=.hg/cache lexbox@{remoteHost}:{remotePath} {repoPath}"
                         """,
        });
        if (process is null)
        {
            return false;
        }

        var errorTask = process.StandardError.ReadToEndAsync(cancellationToken);
        await process.WaitForExitAsync(cancellationToken);
        if (process.ExitCode == 0)
        {
            activity?.SetStatus(ActivityStatusCode.Ok);
            _logger.LogInformation("Migration of repo {Code} finished", project.Code);
            return true;
        }
        var error = await errorTask;
        var description =
            $"rsync for project {project.Code} failed with exit code {process.ExitCode}. Error: {error}.";
        _logger.LogError(description);
        activity?.SetStatus(ActivityStatusCode.Error, description);
        return false;
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
                Path.Combine(_options.Value.RepoPath, code),
                Path.Combine(deletedRepoPath, deletedRepoName));
        });
    }

    private const UnixFileMode Permissions = UnixFileMode.GroupRead | UnixFileMode.GroupWrite |
                                             UnixFileMode.GroupExecute | UnixFileMode.SetGroup |
                                             UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                                             UnixFileMode.SetUser;

    private static void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            target.UnixFileMode = Permissions;
        foreach (var dir in source.EnumerateDirectories())
        {
            CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));
        }

        foreach (var file in source.EnumerateFiles())
        {
            var destFileName = Path.Combine(target.FullName, file.Name);
            var destFile = file.CopyTo(destFileName);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                destFile.UnixFileMode = Permissions;
        }
    }

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
        return Path.Exists(Path.Combine(_options.Value.RepoPath, projectCode, ".hg", "store", "journal"));
    }

    public async Task<string?> GetRepositoryIdentifier(Project project)
    {
        var json = await GetCommit(project.Code, project.MigrationStatus, "0");
        return json?["entries"]?.AsArray().FirstOrDefault()?["node"].Deserialize<string>();
    }

    public async Task<DateTimeOffset?> GetLastCommitTimeFromHg(string projectCode,
        ProjectMigrationStatus migrationStatus)
    {
        var json = await GetCommit(projectCode, migrationStatus, "tip");
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

    private async Task<JsonObject?> GetCommit(string projectCode,
        ProjectMigrationStatus migrationStatus,
        string rev)
    {
        var response = await GetResponseMessage(migrationStatus, projectCode, $"log?style=json-lex&rev={rev}");
        return await response.Content.ReadFromJsonAsync<JsonObject>();
    }

    public async Task<Changeset[]> GetChangesets(string projectCode, ProjectMigrationStatus migrationStatus)
    {
#if DEBUG
        if (migrationStatus is ProjectMigrationStatus.PublicRedmine or ProjectMigrationStatus.PrivateRedmine) return [];
#endif
        var response = await GetResponseMessage(migrationStatus, projectCode, "log?style=json-lex");
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
    }

    public async Task<ProjectType> DetermineProjectType(string projectCode, ProjectMigrationStatus migrationStatus)
    {
        var response = await GetResponseMessage(migrationStatus, projectCode, "file/tip?style=json-lex");
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
    public static string DetermineProjectUrlPrefix(HgType type,
        string projectCode,
        ProjectMigrationStatus migrationStatus,
        HgConfig hgConfig)
    {
        return (type, migrationStatus) switch
        {
            (_, ProjectMigrationStatus.Migrating) => throw new ProjectMigratingException(projectCode),
            //migrated projects
            (HgType.hgWeb, ProjectMigrationStatus.Migrated) => hgConfig.HgWebUrl,
            (HgType.resumable, ProjectMigrationStatus.Migrated) => hgConfig.HgResumableUrl,

            //not migrated projects
            (HgType.hgWeb, ProjectMigrationStatus.PublicRedmine) => hgConfig.PublicRedmineHgWebUrl,
            (HgType.hgWeb, ProjectMigrationStatus.PrivateRedmine) => hgConfig.PrivateRedmineHgWebUrl,
            //all resumable redmine go to the same place
            (HgType.resumable, ProjectMigrationStatus.PublicRedmine) => hgConfig.RedmineHgResumableUrl,
            (HgType.resumable, ProjectMigrationStatus.PrivateRedmine) => hgConfig.RedmineHgResumableUrl,
            _ => throw new ArgumentException(
                $"Unknown request, HG request type: {type}, migration status: {migrationStatus}")
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
