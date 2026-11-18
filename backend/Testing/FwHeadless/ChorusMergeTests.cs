using Chorus.VcsDrivers.Mercurial;
using FluentAssertions;
using SIL.Progress;
using Testing.ApiTests;
using Testing.Services;

namespace Testing.FwHeadless;

/// <summary>
/// Makes FwHeadless merge two hg heads with the real chorusmerge (FieldWorks plugin, as the FwHeadless user).
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
        // Both sides add an item at the same spot: a text merge conflicts there, Chorus's XML merge keeps both
        AppendPartOfSpeech(cloneA, "Added_A");
        AppendPartOfSpeech(cloneB, "Added_B");
        Hg(cloneA, "commit -u test -m \"add Added_A\"");
        Hg(cloneB, "commit -u test -m \"add Added_B\"");
        Hg(cloneA, $"push {RepoUrl}");
        Hg(cloneB, $"push -f {RepoUrl}");

        Hg(cloneA, $"pull {RepoUrl}");
        HeadsOnBranch(cloneA).Should().HaveCount(2);

        // Chorus only merges heads into a new local commit, so give the sync a change to write to FwData.
        // The sync's closing S/R commits that write and merges it with the other head.
        await FwHeadlessTestHelpers.AddCreateEntryCommit(HttpClient, _projectId);
        await FwHeadlessTestHelpers.TriggerSync(HttpClient, _projectId);
        var result = await FwHeadlessTestHelpers.AwaitSyncSuccess(HttpClient, _projectId);
        result!.SyncResult!.FwdataChanges.Should().BeGreaterThan(0, "otherwise there is no closing S/R and no merge");

        Hg(cloneA, $"pull {RepoUrl}");
        HeadsOnBranch(cloneA).Should().ContainSingle();
        ParentsOf(cloneA, FwBranch).Should().HaveCount(2, "the branch head should be a merge commit");
        var mergedPartsOfSpeech = Hg(cloneA, $"cat -r {FwBranch} {PartsOfSpeechFile}");
        mergedPartsOfSpeech.Should().Contain(">Added_A</AUni>").And.Contain(">Added_B</AUni>").And.NotContain("<<<<<<<");
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

    private static void AppendPartOfSpeech(string repoDir, string name)
    {
        var path = Path.Combine(repoDir, PartsOfSpeechFile);
        var content = File.ReadAllText(path);
        const string listEnd = "</Possibilities>";
        content.Should().Contain(listEnd);
        // FLExBridge's commit validation rejects an item missing any basic property
        var item = $"""
            <ownseq class="PartOfSpeech" guid="{Guid.NewGuid()}">
              <BackColor val="-1073741824" />
              <DateCreated val="2026-01-01 00:00:00.000" />
              <DateModified val="2026-01-01 00:00:00.000" />
              <ForeColor val="6303632" />
              <Hidden val="False" />
              <IsProtected val="False" />
              <Name><AUni ws="en">{name}</AUni></Name>
              <SortSpec val="0" />
              <UnderColor val="-1073741824" />
              <UnderStyle val="1" />
            </ownseq>

            """;
        File.WriteAllText(path, content.Replace(listEnd, item + listEnd));
    }

    private static string[] HeadsOnBranch(string repoDir) => Commits(repoDir, $"heads {FwBranch}");

    private static string[] ParentsOf(string repoDir, string rev) => Commits(repoDir, $"parents -r {rev}");

    private static string[] Commits(string repoDir, string hgCommand)
    {
        return Hg(repoDir, $"{hgCommand} --template \"{{node}}\\n\"")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string Hg(string dir, string args)
    {
        var result = HgRunner.Run($"hg {args}", dir, 300, new NullProgress());
        result.ExitCode.Should().Be(0, $"hg {args} failed: {result.StandardError}");
        return result.StandardOutput;
    }
}
