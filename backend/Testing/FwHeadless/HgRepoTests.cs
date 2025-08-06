using Chorus.VcsDrivers.Mercurial;
using SIL.Progress;

namespace Testing.FwHeadless;

public class HgRepoTests : IDisposable
{
    private readonly string _dir;

    public HgRepoTests()
    {
        _dir = Path.GetFullPath(Path.Combine(".", "SyncHgRepoTests"));
    }

    public void Dispose()
    {
        Directory.Delete(_dir, true);
    }

    [Fact]
    public void CanGetHeads()
    {
        var hgRepository = HgRepository.CreateRepositoryInExistingDir(_dir, new NullProgress());
        var testFile = Path.Combine(_dir, "test.txt");
        File.WriteAllText(testFile, "test");
        hgRepository.AddAndCheckinFile(testFile);

        hgRepository.GetHeads().Should().NotBeEmpty();
    }
}
