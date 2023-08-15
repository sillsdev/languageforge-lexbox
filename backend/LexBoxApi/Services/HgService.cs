using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Nodes;
using LexBoxApi.Config;
using LexBoxApi.GraphQL;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using Microsoft.Extensions.Options;
using Path = System.IO.Path;

namespace LexBoxApi.Services;

public class HgService : IHgService
{
    private readonly IOptions<HgConfig> _options;
    private readonly IHttpClientFactory _clientFactory;

    public HgService(IOptions<HgConfig> options, IHttpClientFactory clientFactory)
    {
        _options = options;
        _clientFactory = clientFactory;
    }

    public async Task InitRepo(string code)
    {
        await Task.Run(() => CopyFilesRecursively(
            new DirectoryInfo("Services/HgEmptyRepo"),
            new DirectoryInfo(_options.Value.RepoPath).CreateSubdirectory(code)
        ));
    }

    public async Task DeleteRepo(string code)
    {
        await Task.Run(() => Directory.Delete(Path.Combine(_options.Value.RepoPath, code), true));
    }

    public async Task<string> BackupRepo(string code)
    {
        string tempPath = Path.GetTempPath();
        string timestamp = DateTime.UtcNow.ToString(DateTimeFormatInfo.InvariantInfo.SortableDateTimePattern).Replace(':', '-');
        string baseName = $"backup-{code}-{timestamp}.zip";
        string filename = Path.Join(tempPath, baseName);
        // TODO: Check if a backup has been taken within the past 30 minutes, and return that backup instead of making a new one
        // This would allow resuming an interrupted download
        await Task.Run(() => ZipFile.CreateFromDirectory(Path.Combine(_options.Value.RepoPath, code), filename));
        return filename;
    }

    public async Task<string> ResetRepo(string code)
    {
        string repoPath = Path.Combine(_options.Value.RepoPath, code);
        string timestamp = DateTime.UtcNow.ToString(DateTimeFormatInfo.InvariantInfo.SortableDateTimePattern).Replace(':', '-');
        // var tempFilename = await BackupRepo(code);
        string backupPath = Path.Combine(_options.Value.RepoPath, $"backup-{code}-{timestamp}");
        System.IO.Directory.Move(repoPath, backupPath);
        await InitRepo(code);
        return backupPath;
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

    private void CopyFilesRecursively(DirectoryInfo source, DirectoryInfo target)
    {
        foreach (DirectoryInfo dir in source.GetDirectories())
            CopyFilesRecursively(dir, target.CreateSubdirectory(dir.Name));

        foreach (FileInfo file in source.GetFiles())
            file.CopyTo(Path.Combine(target.FullName, file.Name));
    }

    public async Task<DateTimeOffset?> GetLastCommitTimeFromHg(string projectCode)
    {
        var client = _clientFactory.CreateClient("hgWeg");
        var response = await client.GetAsync($"{_options.Value.HgWebUrl}/hg/{projectCode}/log?style=json-lex&rev=tip");
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

    public async Task<Changeset[]> GetChangesets(string projectCode)
    {
        var client = _clientFactory.CreateClient("hgWeg");
        var response = await client.GetAsync($"{_options.Value.HgWebUrl}/hg/{projectCode}/log?style=json-lex");
        response.EnsureSuccessStatusCode();
        var logResponse = await response.Content.ReadFromJsonAsync<LogResponse>();
        return logResponse?.Changesets ?? Array.Empty<Changeset>();
    }
}



public class LogResponse
{
    public string? Node { get; set; }
    public int? ChangesetCount { get; set; }
    public Changeset[]? Changesets { get; set; }
}
