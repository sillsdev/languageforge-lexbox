using FwLiteShared.LocalSync;

namespace LcmCrdt.Tests.LocalSync;

public sealed class TempFolder : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fwlite-sync-folder-" + Guid.NewGuid().ToString("N"));

    public TempFolder(bool create = true)
    {
        if (create) Directory.CreateDirectory(Path);
    }

    public void Dispose()
    {
        if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
    }
}

public class SyncFolderTests
{
    private static readonly SyncFolderManifest Manifest = new(
        ProjectId: Guid.Parse("6b2d4f80-1a3c-4e5d-9f7b-2c4e6a8d0f13"),
        ProjectCode: "sena-3",
        ProjectName: "Sena 3",
        OriginDomain: "https://lexbox.org",
        WritingSystemIds: ["seh", "en"]);

    [Fact]
    public void AFolderThatDoesNotExistYet_IsEmpty()
    {
        using var folder = new TempFolder(create: false);
        SyncFolder.Inspect(folder.Path).Should().BeOfType<SyncFolderState.Empty>();
    }

    [Fact]
    public void AnEmptyFolder_IsEmpty()
    {
        using var folder = new TempFolder();
        SyncFolder.Inspect(folder.Path).Should().BeOfType<SyncFolderState.Empty>();
    }

    [Fact]
    public void AFolderWithAManifest_HoldsThatProject()
    {
        using var folder = new TempFolder();
        SyncFolder.WriteManifest(folder.Path, Manifest);

        var state = SyncFolder.Inspect(folder.Path);

        state.Should().BeOfType<SyncFolderState.HoldsProject>()
            .Which.Manifest.Should().BeEquivalentTo(Manifest);
    }

    [Fact]
    public void AFolderWithOtherFilesButNoManifest_IsNotASyncFolder()
    {
        using var folder = new TempFolder();
        File.WriteAllText(System.IO.Path.Combine(folder.Path, "holiday photos.txt"), "not a project");

        SyncFolder.Inspect(folder.Path).Should().BeOfType<SyncFolderState.NotASyncFolder>();
    }

    [Fact]
    public void RewritingTheManifest_ReplacesItAndLeavesNoTemporaryFile()
    {
        using var folder = new TempFolder();
        SyncFolder.WriteManifest(folder.Path, Manifest);
        var updated = Manifest with { WritingSystemIds = ["seh", "en", "pt"] };

        SyncFolder.WriteManifest(folder.Path, updated);

        SyncFolder.Inspect(folder.Path).Should().BeOfType<SyncFolderState.HoldsProject>()
            .Which.Manifest.WritingSystemIds.Should().BeEquivalentTo(["seh", "en", "pt"]);
        Directory.EnumerateFiles(folder.Path).Select(System.IO.Path.GetFileName).Should().BeEquivalentTo(["manifest.json"]);
    }

    [Fact]
    public void AManifestFromANewerVersionOfTheApp_IsNotUsed()
    {
        using var folder = new TempFolder();
        SyncFolder.WriteManifest(folder.Path, Manifest with { FormatVersion = SyncFolderManifest.CurrentFormatVersion + 1 });

        SyncFolder.Inspect(folder.Path).Should().BeOfType<SyncFolderState.NewerFormat>();
    }

    [Fact]
    public void ADamagedManifest_IsNotASyncFolder()
    {
        using var folder = new TempFolder();
        SyncFolder.WriteManifest(folder.Path, Manifest with { ProjectCode = "Not A Valid Code" });

        SyncFolder.Inspect(folder.Path).Should().BeOfType<SyncFolderState.NotASyncFolder>();
    }
}
