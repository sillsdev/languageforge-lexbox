using System.Text.Json;
using System.Text.Json.Nodes;
using FwDataMiniLcmBridge;
using FwLiteProjectSync.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm;
using MiniLcm.Models;
using Moq;

namespace FwLiteProjectSync.Tests;

public class ProjectSnapshotServiceTests
{
    private static ProjectSnapshotService CreateService()
    {
        var services = new ServiceCollection()
            .AddSyncServices(nameof(ProjectSnapshotServiceTests));
        return services.BuildServiceProvider()
            .GetRequiredService<ProjectSnapshotService>();
    }

    private static (FwDataProject Project, string ProjectsPath) CreateTempProject()
    {
        var projectsPath = Path.Combine(
            Path.GetTempPath(),
            "FwLiteProjectSync.Tests",
            nameof(ProjectSnapshotServiceTests),
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(projectsPath);
        return (new FwDataProject("proj_" + Guid.NewGuid().ToString("N"), projectsPath), projectsPath);
    }

    private static ProjectSnapshot SnapshotWithSingleVernacularWs(Guid wsId, double order)
    {
        return ProjectSnapshot.Empty with
        {
            WritingSystems = new WritingSystems
            {
                Vernacular =
                [
                    new WritingSystem
                    {
                        Id = wsId,
                        WsId = "en",
                        Type = WritingSystemType.Vernacular,
                        Name = "English",
                        Abbreviation = "EN",
                        Font = "Arial",
                        Order = order
                    }
                ],
                Analysis = []
            }
        };
    }

    [Fact]
    public async Task GetProjectSnapshot_CorruptJson_Throws()
    {
        var snapshotService = CreateService();
        var (project, projectsPath) = CreateTempProject();
        try
        {
            var snapshotPath = ProjectSnapshotService.SnapshotPath(project);
            Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath) ?? projectsPath);
            await File.WriteAllTextAsync(snapshotPath, "{ this is not json");

            Func<Task> act = async () => await snapshotService.GetProjectSnapshot(project);
            await act.Should().ThrowAsync<JsonException>();
        }
        finally
        {
            try { Directory.Delete(projectsPath, recursive: true); } catch { }
        }
    }

    [Fact]
    public async Task SaveProjectSnapshot_KeepBackupTrue_CreatesBackupAndPreservesPreviousContents()
    {
        var (project, projectsPath) = CreateTempProject();
        try
        {
            var snapshotPath = ProjectSnapshotService.SnapshotPath(project);
            Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath) ?? projectsPath);

            await ProjectSnapshotService.SaveProjectSnapshot(project, SnapshotWithSingleVernacularWs(Guid.NewGuid(), order: 1));
            var originalText = await File.ReadAllTextAsync(snapshotPath);

            await ProjectSnapshotService.SaveProjectSnapshot(project, SnapshotWithSingleVernacularWs(Guid.NewGuid(), order: 2), keepBackup: true);

            var backupFiles = Directory.GetFiles(projectsPath, $"{project.Name}_snapshot_backup_*.json");
            backupFiles.Should().ContainSingle("SaveProjectSnapshot(keepBackup:true) should create exactly one backup file");
            var backupText = await File.ReadAllTextAsync(backupFiles.Single());
            backupText.Should().Be(originalText, "backup should preserve the previous snapshot contents exactly");

            var currentText = await File.ReadAllTextAsync(snapshotPath);
            currentText.Should().NotBe(originalText, "the snapshot should have been overwritten with the new content");
        }
        finally
        {
            try { Directory.Delete(projectsPath, recursive: true); } catch { }
        }
    }

    [Fact]
    public async Task SaveProjectSnapshot_IncludesMiniLcmInternalFields()
    {
        var (project, projectsPath) = CreateTempProject();
        try
        {
            var wsId = Guid.NewGuid();
            var snapshot = SnapshotWithSingleVernacularWs(wsId, order: 123.45);
            await ProjectSnapshotService.SaveProjectSnapshot(project, snapshot);

            var snapshotPath = ProjectSnapshotService.SnapshotPath(project);
            await using var stream = File.OpenRead(snapshotPath);
            var node = JsonNode.Parse(stream) ?? throw new InvalidOperationException("Expected JSON to parse");

            var wsNode = node["WritingSystems"]?["Vernacular"]?[0] as JsonObject
                ?? throw new InvalidOperationException("Expected WritingSystems.Vernacular[0] to exist");

            wsNode.ContainsKey("Order").Should().BeTrue("MiniLcmInternal fields should be present in persisted snapshots");
            wsNode["Order"]!.GetValue<double>().Should().Be(123.45);

            wsNode.ContainsKey("MaybeId").Should().BeTrue("MiniLcmInternal fields should be present in persisted snapshots");
            wsNode["MaybeId"]!.GetValue<string>().Should().Be(wsId.ToString());
        }
        finally
        {
            try { Directory.Delete(projectsPath, recursive: true); } catch { }
        }
    }

    [Fact]
    public async Task RegenerateProjectSnapshot_NonCrdtApi_ThrowsInvalidOperationException()
    {
        var snapshotService = CreateService();
        var (project, projectsPath) = CreateTempProject();
        try
        {
            var mockReadApi = new Mock<IMiniLcmReadApi>(MockBehavior.Strict);
            Func<Task> act = async () => await snapshotService.RegenerateProjectSnapshot(mockReadApi.Object, project);
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*CrdtMiniLcmApi*");
        }
        finally
        {
            try { Directory.Delete(projectsPath, recursive: true); } catch { }
        }
    }

    [Fact]
    public void SnapshotPath_UsesProjectsPathRoot_NotProjectFolder()
    {
        var projectsPath = Path.Combine(Path.GetTempPath(), "FwLiteProjectSync.Tests", Guid.NewGuid().ToString("N"));
        var projectName = "proj_" + Guid.NewGuid().ToString("N");
        var project = new FwDataProject(projectName, projectsPath);

        var snapshotPath = ProjectSnapshotService.SnapshotPath(project);
        snapshotPath.Should().Be(Path.Combine(projectsPath, $"{projectName}_snapshot.json"));
        snapshotPath.Should().NotBe(Path.Combine(project.ProjectFolder, $"{projectName}_snapshot.json"));
    }
}

