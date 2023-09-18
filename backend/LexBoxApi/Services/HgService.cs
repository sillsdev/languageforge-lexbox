using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LexCore.Config;
using LexCore.Entities;
using LexCore.Exceptions;
using LexCore.ServiceInterfaces;
using LexCore.Utils;
using LexSyncReverseProxy;
using Microsoft.Extensions.Options;
using Path = System.IO.Path;

namespace LexBoxApi.Services;

public class HgService : IHgService
{
    private const string DELETED_REPO_FOLDER = "_____deleted_____";

    private readonly IOptions<HgConfig> _options;
    private readonly Lazy<HttpClient> _hgClient;

    public HgService(IOptions<HgConfig> options, IHttpClientFactory clientFactory)
    {
        _options = options;
        _hgClient = new(() => clientFactory.CreateClient("HgWeb"));
    }

    /// <summary>
    /// could cause a race condition if HgService is no longer a scoped service
    /// </summary>
    private HttpClient GetClient(ProjectMigrationStatus migrationStatus, string code)
    {
        var client = _hgClient.Value;
        client.BaseAddress = new Uri(DetermineProjectUrlPrefix(HgType.hgWeb, code, migrationStatus, _options.Value));
        if (migrationStatus is ProjectMigrationStatus.PrivateRedmine or ProjectMigrationStatus.PublicRedmine)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.ASCII.GetBytes($"lexbox:{_options.Value.RedmineTrustToken}")));
        }
        else
        {
            client.DefaultRequestHeaders.Authorization = null;
        }
        return client;
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
        await Task.Run(() => CopyFilesRecursively(
            new DirectoryInfo("Services/HgEmptyRepo"),
            new DirectoryInfo(_options.Value.RepoPath).CreateSubdirectory(code)
        ));
    }

    public async Task DeleteRepo(string code)
    {
        await Task.Run(() => Directory.Delete(Path.Combine(_options.Value.RepoPath, code), true));
    }

    public async Task<string?> BackupRepo(string code)
    {
        string repoPath = Path.Combine(_options.Value.RepoPath, code);
        var repoDir = new DirectoryInfo(repoPath);
        if (!repoDir.Exists)
        {
            return null; // Which controller will turn into HTTP 404
        }
        string tempPath = Path.GetTempPath();
        string timestamp = FileUtils.ToTimestamp(DateTime.UtcNow);
        string baseName = $"backup-{code}-{timestamp}.zip";
        string filename = Path.Join(tempPath, baseName);
        // TODO: Check if a backup has been taken within the past 30 minutes, and return that backup instead of making a new one
        // This would allow resuming an interrupted download
        await Task.Run(() => ZipFile.CreateFromDirectory(repoPath, filename));
        return filename;
    }

    public async Task ResetRepo(string code)
    {
        string timestamp = FileUtils.ToTimestamp(DateTimeOffset.UtcNow);
        await SoftDeleteRepo(code, $"{timestamp}__reset");
        await InitRepo(code);
    }

    public async Task RevertRepo(string code, string revHash)
    {
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
            Directory.CreateDirectory(deletedRepoPath);
            Directory.Move(
                Path.Combine(_options.Value.RepoPath, code),
                Path.Combine(deletedRepoPath, deletedRepoName));
        });
    }

    private void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
    {
        foreach (DirectoryInfo dir in source.GetDirectories())
            CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));

        foreach (FileInfo file in source.GetFiles())
            file.CopyTo(Path.Combine(target.FullName, file.Name));
    }

    public async Task<DateTimeOffset?> GetLastCommitTimeFromHg(string projectCode,
        ProjectMigrationStatus migrationStatus)
    {
        var response = await GetClient(migrationStatus, projectCode).GetAsync($"{projectCode}/log?style=json&rev=tip");
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonObject>();
        //format is this: [1678687688, offset] offset is
        var dateArray = json?["entries"]?[0]?["date"].Deserialize<decimal[]>();
        if (dateArray is null || dateArray.Length != 2)
            return null;
        //offsets are weird. The format we get the offset in is opposite of how we typically represent offsets, eg normally the US has negative
        //offsets because it's behind UTC. But in other cases the US has positive offsets because time needs to be added to reach UTC.
        //the offset we get here is the latter but dotnet expects the former so we need to invert it.
        var offset = (double)dateArray[1] * -1;
        var date = DateTimeOffset.FromUnixTimeSeconds((long)dateArray[0]).ToOffset(TimeSpan.FromSeconds(offset));
        return date.ToUniversalTime();
    }

    public async Task<Changeset[]> GetChangesets(string projectCode, ProjectMigrationStatus migrationStatus)
    {
        var response = await GetClient(migrationStatus, projectCode).GetAsync($"{projectCode}/log?style=json");
        response.EnsureSuccessStatusCode();
        var logResponse = await response.Content.ReadFromJsonAsync<LogResponse>();
        return logResponse?.Changesets ?? Array.Empty<Changeset>();
    }

    private void AssertIsSafeRepoName(string name)
    {
        if (string.Equals(name, DELETED_REPO_FOLDER)) throw new ArgumentException($"Invalid repo name: {DELETED_REPO_FOLDER}.");
    }

    public static string DetermineProjectUrlPrefix(HgType type,
        string projectCode,
        ProjectMigrationStatus projectMigrationInfo,
        HgConfig hgConfig)
    {
        return (type, projectMigrationInfo) switch
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
            _ => throw new ArgumentException($"Unknown HG request type: {type}")
        };
    }
}

public class LogResponse
{
    public string? Node { get; set; }
    public int? ChangesetCount { get; set; }
    public Changeset[]? Changesets { get; set; }
}
