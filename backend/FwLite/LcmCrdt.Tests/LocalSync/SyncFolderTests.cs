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
    public void AFolderWithOtherFilesButNoManifest_HoldsOtherFiles()
    {
        using var folder = new TempFolder();
        File.WriteAllText(System.IO.Path.Combine(folder.Path, "holiday photos.txt"), "not a project");

        SyncFolder.Inspect(folder.Path).Should().BeOfType<SyncFolderState.OtherFiles>();
    }

    [Fact]
    public void ADriveRootWithOnlyWindowsSystemFolders_IsEmpty()
    {
        using var folder = new TempFolder();
        Directory.CreateDirectory(System.IO.Path.Combine(folder.Path, "System Volume Information"));
        Directory.CreateDirectory(System.IO.Path.Combine(folder.Path, "$RECYCLE.BIN"));
        File.WriteAllText(System.IO.Path.Combine(folder.Path, ".Trashes"), "");

        SyncFolder.Inspect(folder.Path).Should().BeOfType<SyncFolderState.Empty>();
    }

    [Fact]
    public void OurOwnReadme_DoesNotMakeAFolderNonEmpty()
    {
        using var folder = new TempFolder();
        SyncFolder.WriteReadme(folder.Path, "Sena 3");

        SyncFolder.Inspect(folder.Path).Should().BeOfType<SyncFolderState.Empty>();
        File.ReadAllText(System.IO.Path.Combine(folder.Path, "README.txt")).Should().Contain("\"Sena 3\"");
    }

    [Theory]
    [InlineData("Sena 3", "Sena 3")]
    [InlineData("Sena: 3?", "Sena_ 3_")]
    [InlineData("  Sena   3.  ", "Sena 3")]
    [InlineData("a/b\\c", "a_b_c")]
    [InlineData("???", "___")]
    [InlineData("...", "sena-3")]
    public void TheFolderName_IsSafeOnEveryDriveAndFallsBackToTheCode(string projectName, string expected)
    {
        SyncFolder.FolderNameFor(projectName, "sena-3").Should().Be(expected);
    }

    [Fact]
    public void TheContainer_IsTheLocationItselfWhenTheUserPickedIt()
    {
        SyncFolder.ContainerIn(System.IO.Path.Combine("E:", "FieldWorks Lite")).Should().Be(System.IO.Path.Combine("E:", "FieldWorks Lite"));
        SyncFolder.ContainerIn(System.IO.Path.Combine("E:", "fieldworks lite") + System.IO.Path.DirectorySeparatorChar)
            .Should().Be(System.IO.Path.Combine("E:", "fieldworks lite") + System.IO.Path.DirectorySeparatorChar);
        SyncFolder.ContainerIn("E:").Should().Be(System.IO.Path.Combine("E:", "FieldWorks Lite"));
    }

    [Fact]
    public void ProjectFolders_AreFoundFromTheDriveTheContainerAndTheFolderItself()
    {
        using var drive = new TempFolder();
        var container = System.IO.Path.Combine(drive.Path, SyncFolder.ContainerName);
        var project = System.IO.Path.Combine(container, "Sena 3");
        SyncFolder.WriteManifest(project, Manifest);
        File.WriteAllText(System.IO.Path.Combine(drive.Path, "holiday photos.txt"), "");

        foreach (var picked in new[] { drive.Path, container, project })
        {
            SyncFolder.ProjectFoldersIn(picked).Select(f => f.Path).Should().Equal([project], "picked {0}", picked);
        }
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
    public void AManifestThatFailsValidation_IsDamaged()
    {
        using var folder = new TempFolder();
        SyncFolder.WriteManifest(folder.Path, Manifest with { ProjectCode = "Not A Valid Code" });

        SyncFolder.Inspect(folder.Path).Should().BeOfType<SyncFolderState.DamagedManifest>();
    }

    [Fact]
    public void AManifestThatIsNotJson_IsDamaged()
    {
        using var folder = new TempFolder();
        File.WriteAllText(System.IO.Path.Combine(folder.Path, SyncFolder.ManifestFileName), "{ not json");

        SyncFolder.Inspect(folder.Path).Should().BeOfType<SyncFolderState.DamagedManifest>();
    }
}
