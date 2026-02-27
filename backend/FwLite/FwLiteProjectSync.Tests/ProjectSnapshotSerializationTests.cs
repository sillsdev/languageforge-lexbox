using FwDataMiniLcmBridge;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using FwLiteProjectSync.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;

namespace FwLiteProjectSync.Tests;

public class ProjectSnapshotSerializationTests
{

    private readonly ProjectSnapshotService snapshotService;

    public ProjectSnapshotSerializationTests()
    {
        var crdtServices = new ServiceCollection()
            .AddSyncServices(nameof(ProjectSnapshotSerializationTests));
        snapshotService = crdtServices.BuildServiceProvider()
            .GetRequiredService<ProjectSnapshotService>();
    }

    public static IEnumerable<object[]> GetSena3SnapshotNames()
    {
        return GetSena3SnapshotPaths()
            .Select(file => new object[] { Path.GetFileName(file) });
    }

    private static IEnumerable<string> GetSena3SnapshotPaths()
    {
        var snapshotsDir = RelativePath("Snapshots");
        return Directory.GetFiles(snapshotsDir, "sena-3_snapshot.*.verified.txt");
    }

    [Theory]
    [MemberData(nameof(GetSena3SnapshotNames))]
    public async Task AssertSena3Snapshots(string sourceSnapshotName)
    {
        // arrange
        var fwDataProject = new FwDataProject(
            nameof(AssertSena3Snapshots),
            Path.Combine(".", nameof(ProjectSnapshotSerializationTests)));

        var snapshotPath = ProjectSnapshotService.SnapshotPath(fwDataProject);
        File.Copy(RelativePath($"Snapshots\\{sourceSnapshotName}"), snapshotPath, overwrite: true);

        // act - read the current snapshot
        var snapshot = await snapshotService.GetProjectSnapshot(fwDataProject)
            ?? throw new InvalidOperationException("Failed to load verified snapshot");

        // assert - whatever about the snapshot (i.e. ensure deserialization does what we think)
        snapshot.Entries.Single(e => e.Id == Guid.Parse("cd045907-e8fc-46a3-8f8d-f71bd956275f"))
            .Senses.Single().ExampleSentences.Single().Translations.Single().Text.Should().NotBeEmpty();
    }

    [Fact]
    public async Task LatestSena3SnapshotRoundTrips()
    {
        // arrange
        var latestSnapshotPath = GetSena3SnapshotPaths()
            .OrderDescending()
            .First();

        // act
        var roundTrippedJson = await GetRoundTrippedIndentedSnapshot(latestSnapshotPath);

        // assert
        var verifyName = Path.GetFileName(latestSnapshotPath).Replace(".verified.txt", "");
        await Verify(roundTrippedJson)
            .UseStrictJson()
            .UseDirectory("Snapshots")
            .UseFileName(verifyName);
    }

    private static string RelativePath(string name, [CallerFilePath] string sourceFile = "")
    {
        return Path.Combine(
            Path.GetDirectoryName(sourceFile) ??
            throw new InvalidOperationException("Could not get directory of source file"),
            name);
    }

    private static readonly JsonSerializerOptions IndentedDefaultJsonOptions = new()
    {
        WriteIndented = true,
    };
    private async Task<string> GetRoundTrippedIndentedSnapshot(string sourceSnapshotPath, [CallerMemberName] string fwDataProjectName = "")
    {
        var fwDataProject = new FwDataProject(
            fwDataProjectName,
            Path.Combine(".", nameof(ProjectSnapshotSerializationTests)));

        var snapshotPath = ProjectSnapshotService.SnapshotPath(fwDataProject);
        Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath) ?? throw new InvalidOperationException("Could not get directory of snapshot path"));
        File.Copy(sourceSnapshotPath, snapshotPath, overwrite: true);

        var snapshot = await snapshotService.GetProjectSnapshot(fwDataProject)
            ?? throw new InvalidOperationException("Failed to load verified snapshot");
        await ProjectSnapshotService.SaveProjectSnapshot(fwDataProject, snapshot);

        using var stream = File.OpenRead(snapshotPath);
        var node = JsonNode.Parse(stream, null, new()
        {
            AllowTrailingCommas = true,
        }) ?? throw new InvalidOperationException("Could not parse json node");
        return node.ToJsonString(IndentedDefaultJsonOptions);
    }
}
