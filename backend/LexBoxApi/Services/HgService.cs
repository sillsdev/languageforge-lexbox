using LexBoxApi.Config;
using Microsoft.Extensions.Options;

namespace LexBoxApi.Services;

public class HgService
{
    private readonly IOptions<HgConfig> _options;

    public HgService(IOptions<HgConfig> options)
    {
        _options = options;
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
}