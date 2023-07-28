using System.Text.Json;
using System.Text.Json.Nodes;
using LexBoxApi.Config;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using Microsoft.Extensions.Options;
using Path = System.IO.Path;

namespace LexBoxApi.Services;

public class HgService : IHgService
{
    private const string DELETED_REPO_FOLDER = "_____deleted_____";

    private readonly IOptions<HgConfig> _options;
    private readonly IHttpClientFactory _clientFactory;

    public HgService(IOptions<HgConfig> options, IHttpClientFactory clientFactory)
    {
        _options = options;
        _clientFactory = clientFactory;
    }

    /// <summary>
    /// Note: The repo is unstable and potentially unavailable for a short while after creation, so don't read from it right away.
    /// See: https://github.com/sillsdev/languageforge-lexbox/issues/173#issuecomment-1665478630
    /// </summary>
    public async Task InitRepo(string code)
    {
        AssertIsSafeRepoName(code);
        await Task.Run(() => CopyFilesRecursively(
            new DirectoryInfo("Services/HgEmptyRepo"),
            new DirectoryInfo(_options.Value.RepoPath).CreateSubdirectory(code)
        ));
    }

    public async Task DeleteRepo(string code)
    {
        await Task.Run(() => Directory.Delete(Path.Combine(_options.Value.RepoPath, code), true));
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

    private void AssertIsSafeRepoName(string name)
    {
        if (string.Equals(name, DELETED_REPO_FOLDER)) throw new ArgumentException($"Invalid repo name: {DELETED_REPO_FOLDER}.");
    }
}

public class LogResponse
{
    public string? Node { get; set; }
    public int? ChangesetCount { get; set; }
    public Changeset[]? Changesets { get; set; }
}
