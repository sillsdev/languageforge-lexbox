using FwDataMiniLcmBridge.Media;
using Microsoft.Extensions.Logging;
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

    // FW stores audio references in NFD via MiniLcm's normalisation wrapper. A real user's
    // LinkedFiles\AudioVisual directory was observed to contain both an NFC and an NFD copy of
    // the same audio (e.g. süülda.wav written on Windows alongside its NFD twin from a macOS
    // hg pull). UUIDNext.NewNameBased normalises before hashing, so both copies produce the same
    // FileId — and the dictionary build in LocalMediaAdapter.Paths used to throw
    // "An item with the same key has already been added", crashing every entry fetch.
    [Fact]
    public void EnumerationOfNfcAndNfdTwinsDoesNotCrash()
    {
        // explicit escape sequences so the source file's encoding cannot collapse the two literals.
        const string nfc = "süülda.wav";          // precomposed ü
        const string nfd = "süülda.wav";        // u + combining diaeresis
        nfc.Should().NotBe(nfd, "test setup is vacuous if the two literals are equal");

        File.WriteAllBytes(Path.Combine(_audioVisual, nfc), [1]);
        File.WriteAllBytes(Path.Combine(_audioVisual, nfd), [2]);
        Directory.EnumerateFiles(_audioVisual).Count().Should().Be(2, "both physical files must coexist on disk");

        var dict = LocalMediaAdapter.BuildPathsDictionary(_root, NullLogger<LocalMediaAdapter>.Instance);

        // The build did not throw, and the surviving entry resolves to a real file (whichever
        // enumeration order returned first — FW's NFD lookup will hit it regardless because
        // UUIDNext gives both twins the same FileId).
        dict.Should().HaveCount(1);
        File.Exists(dict.Single().Value).Should().BeTrue();
    }
}
