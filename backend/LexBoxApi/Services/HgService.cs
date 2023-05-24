using System.Text.Json;
using System.Text.Json.Nodes;
using LexBoxApi.Config;
using Microsoft.Extensions.Options;
using Path = System.IO.Path;

namespace LexBoxApi.Services;

public interface IHgService
{
    Task InitRepo(string code);
    Task<DateTimeOffset?> GetLastCommitTimeFromHg(string projectCode);
}

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
        var response = await client.GetAsync($"{_options.Value.HgWebUrl}/hg/{projectCode}/log?style=json&rev=tip");
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
}