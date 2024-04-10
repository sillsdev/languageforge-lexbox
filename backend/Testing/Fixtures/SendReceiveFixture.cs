using System.IO.Compression;
using Testing.ApiTests;
using static Testing.Services.Constants;

namespace Testing.Fixtures;

public class SendReceiveFixture : IAsyncLifetime
{

    public ApiTestBase AdminApiTester { get; } = new();

    public async Task InitializeAsync()
    {
        DeletePreviousTestFiles();
        await DownloadTemplateRepo();
        await AdminApiTester.LoginAs(AdminAuth.Username, AdminAuth.Password);
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    private static void DeletePreviousTestFiles()
    {
        if (Directory.Exists(BasePath)) Directory.Delete(BasePath, true);
    }

    private async Task DownloadTemplateRepo()
    {
        await using var stream = await AdminApiTester.HttpClient.GetStreamAsync("https://drive.google.com/uc?export=download&id=1w357T1Ti7bDwEof4HPBUZ5gB7WSKA5O2");
        using var zip = new ZipArchive(stream);
        zip.ExtractToDirectory(TemplateRepo.FullName);
    }
}
