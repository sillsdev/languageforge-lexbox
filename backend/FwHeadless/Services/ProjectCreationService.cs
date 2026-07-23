using System.Xml;
using System.Xml.Linq;
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.LcmUtils;
using LexCore.Exceptions;
using Microsoft.Extensions.Options;
using SIL.Xml;

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

    // The one file FieldWorks/FLExBridge put in a brand-new FLEx repo's first commit: a minimal custom
    // property definition list. Mirrors FlexBridgeConstants.CustomPropertiesFilename (internal in FLExBridge).
    private const string CustomPropertiesFilename = "FLExProject.CustomProperties";

    public async Task CreateFromTemplate(
        Guid projectId,
        string projectCode,
        IReadOnlyList<string> vernacularWritingSystems,
        IReadOnlyList<string> analysisWritingSystems,
        string uiWritingSystem)
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
            // Build the first commit of a brand-new FLEx repo from scratch, mirroring how FieldWorks/
            // FLExBridge create a repo, since Clone can't establish an empty remote and
            // Language_Forge_Send_Receive refuses to make the *first* commit itself:
            //   1. hg init the local fw/ folder (no clone -- the empty remote has nothing to clone).
            //   2. Build fw.fwdata into that folder from the SIL.LCModel template, configured with all
            //      of the requested writing systems.
            //   3. Commit a minimal FLExProject.CustomProperties file on hg's *default* branch -- the same
            //      genesis file, on the same branch, FieldWorks commits first. The fwdata itself is NOT
            //      committed; Send/Receive splits it into the nested files Mercurial actually tracks
            //      (fwdata is excluded from tracking).
            //   4. Set the branch to the send/receive branch name (FlexBridgeDataVersion.modelVersion,
            //      e.g. 7500002.7000072) AFTER the genesis commit; FLEx/LfMergeBridge look for the data on
            //      that branch, so the split data must land there while the genesis stays on 'default'.
            //   5. Send/Receive: split fw.fwdata into its nested files, commit those on the branch, push.
            await srService.InitRepo(fwDataProject.ProjectFolder);

            BuildFromTemplate(fwDataProject, vernacularWritingSystems, analysisWritingSystems, uiWritingSystem);
            FixLinkedFilesRootDirSeparator(fwDataProject);
            AssertModelVersionMatches(fwDataProject);

            var customPropertiesFile = WriteInitialCustomPropertiesFile(fwDataProject);
            await srService.CommitFile(customPropertiesFile, InitialCommitMessage);

            await srService.SetBranch(fwDataProject.ProjectFolder, config.Value.SendReceiveBranchName);
            // BUG WORKAROUND (remove once the LanguageForgeSendReceiveActionHandler bug is fixed): that
            // handler refuses to commit when doing so "could possibly create a new branch", so it won't
            // establish the model-version branch itself. This empty commit (no file changes -- it only
            // records the branch change from SetBranch above) creates a head on the branch so Send/Receive
            // will proceed.
            await srService.CommitEmpty(fwDataProject.ProjectFolder, InitialCommitMessage);
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

    /// <summary>
    /// FieldWorks (Windows) compares LangProject.LinkedFilesRootDir with an exact string, expecting the
    /// Windows-style default <c>%proj%\LinkedFiles</c>. liblcm builds that path with Path.Combine, which on
    /// this Linux host yields <c>%proj%/LinkedFiles</c> (forward slash), so FieldWorks warns on its first
    /// Send/Receive. Rewrite just that one value to the backslash form.
    ///
    /// We deliberately do NOT parse the (potentially multi-MB) fwdata as XML -- that would be a needless
    /// cost. The value always sits on the line immediately after the &lt;LinkedFilesRootDir&gt; open tag,
    /// so stream the file once, fix the &lt;Uni&gt; on the line following that tag, and copy every other
    /// line through untouched.
    /// </summary>
    private static void FixLinkedFilesRootDirSeparator(FwDataProject fwDataProject)
    {
        const string markerLinePrefix = "<LinkedFilesRootDir";
        const string forwardSlashValue = "<Uni>%proj%/LinkedFiles</Uni>";
        const string backslashValue = @"<Uni>%proj%\LinkedFiles</Uni>";

        var sourcePath = fwDataProject.FilePath;
        var tempPath = sourcePath + ".tmp";
        using (var reader = new StreamReader(sourcePath))
        using (var writer = new StreamWriter(tempPath))
        {
            var nextLineIsValue = false;
            var done = false;
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                if (nextLineIsValue)
                {
                    line = line.Replace(forwardSlashValue, backslashValue);
                    nextLineIsValue = false;
                    done = true;
                }
                else if (!done && line.StartsWith(markerLinePrefix, StringComparison.Ordinal))
                {
                    nextLineIsValue = true;
                }
                writer.WriteLine(line);
            }
        }
        File.Move(tempPath, sourcePath, overwrite: true);
    }

    /// <summary>
    /// Writes the genesis FLExProject.CustomProperties file into the repo root: an empty custom-property
    /// list (just an &lt;AdditionalFields /&gt; root). Uses SIL's canonical XML settings so the bytes match
    /// exactly what the first Send/Receive's project splitter regenerates, leaving no spurious diff.
    /// </summary>
    private string WriteInitialCustomPropertiesFile(FwDataProject fwDataProject)
    {
        var path = Path.Join(fwDataProject.ProjectFolder, CustomPropertiesFilename);
        using (var writer = XmlWriter.Create(path, CanonicalXmlSettings.CreateXmlWriterSettings()))
        {
            writer.WriteStartDocument();
            new XElement("AdditionalFields").WriteTo(writer);
        }
        return path;
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
