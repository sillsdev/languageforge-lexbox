using System.Xml;
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.LcmUtils;
using LexCore.Entities;
using LexCore.Exceptions;
using Microsoft.Extensions.Options;

namespace FwHeadless.Services;

/// <summary>
/// Creates a brand-new FieldWorks project from the SIL.LCModel template and pushes it into the
/// (empty) Mercurial repo that LexBox has already initialised for the project.
/// </summary>
public class ProjectCreationService(
    IOptions<FwHeadlessConfig> config,
    ISendReceiveService srService,
    IProjectLoader projectLoader,
    SyncHostedService syncHostedService,
    ILogger<ProjectCreationService> logger)
{
    private const string InitialCommitMessage = "Initial project creation";

    public async Task CreateFromTemplate(
        Guid projectId,
        string projectCode,
        IReadOnlyList<string> vernacularWritingSystems,
        IReadOnlyList<string> analysisWritingSystems,
        string uiWritingSystem,
        AnthropologyCategories anthropologyCategories)
    {
        if (vernacularWritingSystems.Count == 0)
            throw new ArgumentException("At least one vernacular writing system is required", nameof(vernacularWritingSystems));
        if (analysisWritingSystems.Count == 0)
            throw new ArgumentException("At least one analysis writing system is required", nameof(analysisWritingSystems));
        if (string.IsNullOrEmpty(uiWritingSystem))
            throw new ArgumentException("A UI writing system is required", nameof(uiWritingSystem));

        // Reserve the project so a concurrent create or sync can't race on the same fw/ folder and repo.
        if (!syncHostedService.TryStartProjectCreation(projectId))
            throw new ProjectSyncInProgressException(projectId);
        var fwDataProject = config.Value.GetFwDataProject(projectCode, projectId);
        try
        {
            // Build the first commit of a brand-new FLEx repo from scratch, mirroring LfMerge's genesis
            // recipe (hg init -> branch -> write files -> commit) since Clone can't establish an empty
            // remote and Language_Forge_Send_Receive refuses to make the *first* commit itself:
            //   1. hg init the local fw/ folder (no clone -- the empty remote has nothing to clone).
            //   2. Set the branch to the FDO model version BEFORE any commit; FLEx clients look for the
            //      data on that branch, so a first commit on 'default' would be invisible to them.
            //   3. Build fw.fwdata into that folder from the SIL.LCModel template, configured with all
            //      of the requested writing systems.
            //   4. hg add + commit the (untracked) fwdata onto the model-version branch, then push.
            await srService.InitRepo(fwDataProject.ProjectFolder);
            await srService.SetBranch(fwDataProject.ProjectFolder, config.Value.FdoDataModelVersion);

            BuildFromTemplate(fwDataProject, vernacularWritingSystems, analysisWritingSystems, uiWritingSystem);
            ApplyAnthropologyCategories(anthropologyCategories);
            AssertModelVersionMatches(fwDataProject);

            await srService.CommitFile(fwDataProject.FilePath, InitialCommitMessage);
            var pushResult = await srService.SendReceive(fwDataProject, projectCode, InitialCommitMessage);
            if (!pushResult.Success)
                throw new SendReceiveException("Pushing the new project to the repo failed", pushResult);

            logger.LogInformation("Created project {ProjectCode} ({ProjectId}) from template", projectCode, projectId);
        }
        catch
        {
            CleanupLocalProject(fwDataProject);
            throw;
        }
        finally
        {
            syncHostedService.EndProjectCreation(projectId);
        }
    }

    private void BuildFromTemplate(
        FwDataProject fwDataProject,
        IReadOnlyList<string> vernacularWritingSystems,
        IReadOnlyList<string> analysisWritingSystems,
        string uiWs)
    {
        // NewProject copies the SIL.LCModel template, configured with all of the requested writing
        // systems (the first of each list is the default of that type), and returns a loaded cache;
        // dispose it right away so its file locks are released before the following hg add/commit.
        using var cache = projectLoader.NewProject(fwDataProject, analysisWritingSystems, vernacularWritingSystems, uiWs);
    }

    private void ApplyAnthropologyCategories(AnthropologyCategories anthropologyCategories)
    {
        // TODO: Implement — populate the project with the requested anthropology categories
        // (enhanced/standard). Currently a no-op; 'none' is the only fully-handled option.
        if (anthropologyCategories != AnthropologyCategories.None)
            logger.LogWarning("anthropologyCategories '{Categories}' was requested but is not implemented yet; " +
                "the new project will have no anthropology categories", anthropologyCategories);
    }

    /// <summary>
    /// The freshly-created project is stamped with the SIL.LCModel package's data-model version.
    /// If that ever diverges from the version FwHeadless syncs with, pushing it would corrupt data
    /// (see FwHeadless AGENTS.md), so fail before the push rather than after.
    /// </summary>
    private void AssertModelVersionMatches(FwDataProject fwDataProject)
    {
        var expected = config.Value.FdoDataModelVersion;
        var actual = ReadModelVersion(fwDataProject.FilePath);
        // Fail closed: an unreadable version ('actual' == null) is treated as a mismatch, since this is
        // the last safety net before an irreversible push in a data-loss-risk area.
        if (actual != expected)
        {
            throw new InvalidOperationException(
                $"New project's data-model version '{actual ?? "(unreadable)"}' does not match FwHeadless " +
                $"FdoDataModelVersion '{expected}'; refusing to push to avoid data corruption.");
        }
    }

    private static string? ReadModelVersion(string fwDataFilePath)
    {
        using var reader = XmlReader.Create(fwDataFilePath, new XmlReaderSettings { IgnoreComments = true, IgnoreWhitespace = true });
        // The .fwdata root element carries the FDO model version, e.g. <languageproject version="7000072">.
        return reader.MoveToContent() == XmlNodeType.Element ? reader.GetAttribute("version") : null;
    }

    private void CleanupLocalProject(FwDataProject fwDataProject)
    {
        // Mirror SyncWorker.SetupFwData's failure cleanup so a retry starts clean. The remote repo
        // (created by LexBox) is torn down by the caller on the LexBox side.
        try
        {
            if (Directory.Exists(fwDataProject.ProjectFolder))
                Directory.Delete(fwDataProject.ProjectFolder, true);
            if (Directory.Exists(fwDataProject.ProjectsPath) &&
                !Directory.EnumerateFileSystemEntries(fwDataProject.ProjectsPath).Any())
                Directory.Delete(fwDataProject.ProjectsPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to clean up local project folder after a failed creation: {ProjectFolder}",
                fwDataProject.ProjectFolder);
        }
    }
}
