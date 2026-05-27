using System.Text;
using FwDataMiniLcmBridge.Media;
using Microsoft.Extensions.Logging.Abstractions;

namespace FwDataMiniLcmBridge.Tests;

public class LocalMediaAdapterTests : IDisposable
{
    private readonly string _root;
    private readonly string _audioVisual;

    public LocalMediaAdapterTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "LocalMediaAdapterTests-" + Guid.NewGuid().ToString("N")[..8]);
        _audioVisual = Path.Combine(_root, "AudioVisual");
        Directory.CreateDirectory(_audioVisual);
    }

    public void Dispose()
    {
        if (Directory.Exists(_root)) Directory.Delete(_root, recursive: true);
    }

    // NFC + NFD twins of the same audio collapse to a single entry (UUIDNext.NewNameBased
    // normalises before hashing -> same FileId). The kept path is the NFD one, since
    // that's the only form FW addresses audio with.
    [Fact]
    public void NfcAndNfdTwinsCollapseToNfd()
    {
        const string nfc = "süülda.wav";
        const string nfd = "süülda.wav";
        nfc.Should().Be(nfc.Normalize(NormalizationForm.FormC));
        nfd.Should().Be(nfd.Normalize(NormalizationForm.FormD));
        nfc.Should().NotBe(nfd, "test setup is vacuous if the two literals are equal");

        File.WriteAllBytes(Path.Combine(_audioVisual, nfc), [1]);
        File.WriteAllBytes(Path.Combine(_audioVisual, nfd), [2]);
        Directory.EnumerateFiles(_audioVisual).Count().Should().Be(2, "both physical files must coexist on disk");

        var dict = LocalMediaAdapter.BuildPathsDictionary(_root, NullLogger<LocalMediaAdapter>.Instance);

        dict.Should().HaveCount(1);
        File.Exists(dict.Single().Value).Should().BeTrue();
        dict.Single().Value.Should().EndWith(nfd);
    }
}
