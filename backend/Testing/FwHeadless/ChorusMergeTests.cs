using Chorus.VcsDrivers.Mercurial;
using FluentAssertions;
using SIL.Progress;
using Testing.ApiTests;
using Testing.Services;

namespace Testing.FwHeadless;

/// <summary>
/// Makes FwHeadless merge two hg heads with the real chorusmerge (FieldWorks plugin, as the FwHeadless user).
/// Chorus only merges heads against a local commit, so the merge happens in the closing S/R,
/// which runs only when the CRDT sync writes to FwData.
/// </summary>
[Trait("Category", "Integration")]
[Collection("FwHeadless Sync")]
public class ChorusMergeTests : ApiTestBase, IAsyncLifetime
{
    // sena-3's tip is on this named branch; rev 0 is on default, so heads must be counted per branch
    private const string FwBranch = "7500002.7000072";
    private const string PartsOfSpeechFile = "Linguistics/MorphologyAndSyntax/PartsOfSpeech.list";

    private Guid _projectId;
    private string _projectCode = null!;
    private DirectoryInfo _workDir = null!;

    public async Task InitializeAsync()
    {
        await LoginAs("admin");
        _projectCode = Utils.NewProjectCode();
        _projectId = await FwHeadlessTestHelpers.CopyProjectToNewProject(HttpClient, _projectCode, "sena-3");
        _workDir = Directory.CreateTempSubdirectory("chorus-merge-test-");
    }

    public async Task DisposeAsync()
    {
        await HttpClient.DeleteAsync($"api/project/{_projectId}");
        _workDir.Delete(recursive: true);
    }

    [Fact]
    public async Task ClosingSendReceive_MergesDivergentHeadsWithChorusMerge()
    {
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, _projectId);
        await FwHeadlessTestHelpers.AwaitSyncSuccess(HttpClient, _projectId);

        var cloneA = CloneAndUpdateToTip("a");
        var cloneB = CloneAndUpdateToTip("b");
        ReplaceInFile(cloneA, PartsOfSpeechFile, ">Adverb</AUni>", ">Adverb_A</AUni>");
        ReplaceInFile(cloneB, PartsOfSpeechFile, ">Verb</AUni>", ">Verb_B</AUni>");
        Hg(cloneA, "commit -u test -m \"rename Adverb\"");
        Hg(cloneB, "commit -u test -m \"rename Verb\"");
        Hg(cloneA, $"push {RepoUrl}");
        Hg(cloneB, $"push -f {RepoUrl}");

        Hg(cloneA, $"pull {RepoUrl}");
        HeadsOnBranch(cloneA).Should().HaveCount(2);

        await FwHeadlessTestHelpers.AddCreateEntryCommit(HttpClient, _projectId);
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, _projectId);
        var result = await FwHeadlessTestHelpers.AwaitSyncSuccess(HttpClient, _projectId);
        result!.SyncResult!.FwdataChanges.Should().BeGreaterThan(0, "otherwise there is no closing S/R and no merge");

        Hg(cloneA, $"pull {RepoUrl}");
        HeadsOnBranch(cloneA).Should().HaveCount(1);
        var mergedPartsOfSpeech = Hg(cloneA, $"cat -r {FwBranch} {PartsOfSpeechFile}");
        mergedPartsOfSpeech.Should().Contain(">Adverb_A</AUni>").And.Contain(">Verb_B</AUni>");
    }

    private string RepoUrl
    {
        get
        {
            var url = new UriBuilder($"{TestingEnvironmentVariables.StandardHgBaseUrl}/{_projectCode}")
            {
                UserName = "admin",
                Password = TestingEnvironmentVariables.DefaultPassword
            };
            return url.ToString();
        }
    }

    private string CloneAndUpdateToTip(string name)
    {
        var dir = Path.Combine(_workDir.FullName, name);
        Hg(_workDir.FullName, $"clone {RepoUrl} {name}");
        // clone updates to the default branch, not to the FW data branch
        Hg(dir, "update tip");
        return dir;
    }

    private static void ReplaceInFile(string repoDir, string relativePath, string oldValue, string newValue)
    {
        var path = Path.Combine(repoDir, relativePath);
        var content = File.ReadAllText(path);
        content.Should().Contain(oldValue);
        File.WriteAllText(path, content.Replace(oldValue, newValue));
    }

    private static string[] HeadsOnBranch(string repoDir)
    {
        return Hg(repoDir, $"heads {FwBranch} --template \"{{node}}\\n\"")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string Hg(string dir, string args)
    {
        var result = HgRunner.Run($"hg {args}", dir, 300, new NullProgress());
        result.ExitCode.Should().Be(0, $"hg {args} failed: {result.StandardError}");
        return result.StandardOutput;
    }
}
