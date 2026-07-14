using System.Xml;
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.LcmUtils;
using LexCore.Entities;
using LexCore.Exceptions;
using Microsoft.Extensions.Options;
using MiniLcm.Models;

namespace FwHeadless.Services;

/// <summary>
/// Creates a brand-new FieldWorks project from the SIL.LCModel template and pushes it into the
/// (empty) Mercurial repo that LexBox has already initialised for the project.
/// </summary>
public class ProjectCreationService(
    IOptions<FwHeadlessConfig> config,
    ISendReceiveService srService,
    FwDataFactory fwDataFactory,
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
        AnthropologyCategories anthropologyCategories)
    {
        if (vernacularWritingSystems.Count == 0)
            throw new ArgumentException("At least one vernacular writing system is required", nameof(vernacularWritingSystems));
        if (analysisWritingSystems.Count == 0)
            throw new ArgumentException("At least one analysis writing system is required", nameof(analysisWritingSystems));

        // Reserve the project so a concurrent create or sync can't race on the same fw/ folder and repo.
        if (!syncHostedService.TryStartProjectCreation(projectId))
            throw new ProjectSyncInProgressException(projectId);
        var fwDataProject = config.Value.GetFwDataProject(projectCode, projectId);
        try
        {
            // Populate the empty repo using only already-exercised FwHeadless primitives:
            //   1. Clone the empty remote -> local fw/ folder (.hg, no fwdata yet).
            //   2. Build fw.fwdata into that folder from the SIL.LCModel template.
            //   3. Add the remaining writing systems, saving and releasing the LCM file locks.
            //   4. hg add + commit the (untracked) fwdata, then push.
            var cloneResult = await srService.Clone(fwDataProject, projectCode);
            if (!cloneResult.Success)
                throw new SendReceiveException("Clone of the new project's repo failed", cloneResult);

            BuildFromTemplate(fwDataProject, vernacularWritingSystems[0], analysisWritingSystems[0]);
            await AddWritingSystems(fwDataProject, vernacularWritingSystems, analysisWritingSystems);
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

    private void BuildFromTemplate(FwDataProject fwDataProject, string vernacularWs, string analysisWs)
    {
        // NewProject copies the SIL.LCModel template (with the first vernacular + analysis WS) and
        // returns a loaded cache; dispose it right away so its file locks are released before we
        // re-open the project through FwDataFactory to add the remaining writing systems.
        using var cache = projectLoader.NewProject(fwDataProject, analysisWs, vernacularWs);
    }

    private async Task AddWritingSystems(
        FwDataProject fwDataProject,
        IReadOnlyList<string> vernacularWritingSystems,
        IReadOnlyList<string> analysisWritingSystems)
    {
        // The template already has the first vernacular + first analysis WS; add the rest, deduping
        // against what's present. saveOnDispose flushes the additions to the .fwdata XML when the api
        // is disposed; CloseProjectAsync then disposes the underlying LcmCache so the following hg
        // add/commit doesn't hit a file lock.
        try
        {
            using (var api = fwDataFactory.GetFwDataMiniLcmApi(fwDataProject, saveOnDispose: true))
            {
                var existing = await api.GetWritingSystems();
                var vernacularPresent = existing.Vernacular.Select(ws => ws.WsId).ToHashSet();
                var analysisPresent = existing.Analysis.Select(ws => ws.WsId).ToHashSet();
                foreach (var code in vernacularWritingSystems)
                    await AddWritingSystem(api, code, WritingSystemType.Vernacular, vernacularPresent);
                foreach (var code in analysisWritingSystems)
                    await AddWritingSystem(api, code, WritingSystemType.Analysis, analysisPresent);
            }
        }
        finally
        {
            await fwDataFactory.CloseProjectAsync(fwDataProject);
        }
    }

    private static async Task AddWritingSystem(FwDataMiniLcmApi api, string code, WritingSystemType type, HashSet<WritingSystemId> present)
    {
        var wsId = new WritingSystemId(code);
        if (!present.Add(wsId)) return; // already present (the template's first WS, or a repeated request)
        await api.CreateWritingSystem(new WritingSystem
        {
            Id = Guid.Empty,
            WsId = wsId,
            Name = code,
            Abbreviation = code,
            Font = "Charis SIL",
            Type = type,
        });
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
