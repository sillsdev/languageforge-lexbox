using System.Text;
using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.Media;
using FwDataMiniLcmBridge.Tests.Fixtures;
using Microsoft.Extensions.DependencyInjection;
using MiniLcm.Media;
using MiniLcm.Models;
using SIL.LCModel.Infrastructure;

namespace FwDataMiniLcmBridge.Tests;

[Collection(ProjectLoaderFixture.Name)]
public class MediaFileTests : IAsyncLifetime
{
    private readonly FwDataMiniLcmApi _api;
    private readonly WritingSystemId _audioWs = "en-Zxxx-x-audio";
    private IMediaAdapter _mediaAdapter;

    public MediaFileTests(ProjectLoaderFixture fixture)
    {
        _mediaAdapter = fixture.Services.GetRequiredService<IMediaAdapter>();
        _api = fixture.NewProjectApi("media-file-test", "en", "en");
    }

    public async Task InitializeAsync()
    {
        Directory.CreateDirectory(Path.Combine(_api.Cache.LangProject.LinkedFilesRootDir,
            FwDataMiniLcmApi.AudioVisualFolder));
        await _api.CreateWritingSystem(new WritingSystem
        {
            Id = Guid.NewGuid(),
            WsId = _audioWs,
            Name = "English Audio",
            Abbreviation = "EN (A)",
            Font = "Arial",
            Type = WritingSystemType.Vernacular
        });
    }

    public Task DisposeAsync()
    {
        var projectFolder = _api.Cache.ProjectId.ProjectFolder;
        _api.Dispose();
        if (Directory.Exists(projectFolder)) Directory.Delete(projectFolder, true);
        return Task.CompletedTask;
    }

    private async Task<Guid> AddFileDirectly(string fileName, string? contents, bool storeFile = true)
    {
        if (storeFile)
            await StoreFileContentsAsync(fileName, contents);

        var entry = await _api.CreateEntry(new Entry() { LexemeForm = { ["en"] = "test" } });
        var lexEntry = _api.EntriesRepository.GetObject(entry.Id);
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Set CitationForm audio",
            "Undo setting CitationForm audio",
            _api.Cache.ServiceLocator.ActionHandler,
            () =>
            {
                lexEntry.CitationForm.set_String(_api.GetWritingSystemHandle(_audioWs, WritingSystemType.Vernacular),
                    fileName);
            });
        return entry.Id;
    }

    private async Task<Guid> StoreFileContentsAsync(string fileName, string? contents)
    {
        var filePath = Path.Combine(_api.Cache.LangProject.LinkedFilesRootDir, FwDataMiniLcmApi.AudioVisualFolder, fileName);
        await File.WriteAllTextAsync(filePath, contents);
        //using media adapter to ensure it's cache is updated with the new file
        return _mediaAdapter.MediaUriFromPath(filePath, _api.Cache).FileId;
    }

    private string GetFwAudioValue(Guid id)
    {
        var filePath = _api.EntriesRepository.GetObject(id).CitationForm
            .get_String(_api.GetWritingSystemHandle(_audioWs, WritingSystemType.Vernacular))
            .Text;
        return filePath;
    }

    [Fact]
    public async Task GetEntry_MapsFilePathsFromAudioWs()
    {
        var fileName = "MapsAFileReferenceIntoAMediaUri.txt";
        var fileGuid = LocalMediaAdapter.NewGuidV5(Path.Combine(_api.Cache.LangProject.LinkedFilesRootDir, FwDataMiniLcmApi.AudioVisualFolder, fileName));
        var entryId = await AddFileDirectly(fileName, "test");

        var entry = await _api.GetEntry(entryId);

        entry.Should().NotBeNull();
        entry.CitationForm[_audioWs].Should().Be(new MediaUri(fileGuid, "localhost").ToString());
        GetFwAudioValue(entryId).Should().Be(fileName);
    }

    [Fact]
    public async Task CreateEntry_MapsMediaUrisForAudioWs()
    {
        var fileName = "CreateEntry_MapsMediaUrisForAudioWs.txt";
        var fileId = await StoreFileContentsAsync(fileName, "test");
        var mediaUri = new MediaUri(fileId, "localhost");
        var entry = await _api.CreateEntry(new Entry()
        {
            LexemeForm = { ["en"] = "test" }, CitationForm = { [_audioWs] = mediaUri.ToString() }
        });

        var fwAudioValue = GetFwAudioValue(entry.Id);
        fwAudioValue.Should().Be(fileName);
        entry.CitationForm[_audioWs].Should().Be(mediaUri.ToString());
    }

    [Fact]
    public async Task UpdateEntry_MapsMediaUrisForAudioWs()
    {
        var fileName = "UpdateEntry_MapsMediaUrisForAudioWs.txt";
        var fileId = await StoreFileContentsAsync(fileName, "test");
        var mediaUri = new MediaUri(fileId, "localhost");
        var entry = await _api.CreateEntry(new Entry() { LexemeForm = { ["en"] = "test" } });
        entry.Should().NotBeNull();

        var after = entry.Copy();
        after.CitationForm[_audioWs] = mediaUri.ToString();
        await _api.UpdateEntry(entry, after);

        entry = await _api.GetEntry(entry.Id);

        entry.Should().NotBeNull();
        entry.CitationForm[_audioWs].Should().Be(mediaUri.ToString());
        var fwAudioValue = GetFwAudioValue(entry.Id);
        fwAudioValue.Should().Be(fileName);
    }

    [Fact]
    public async Task AudioWsValuesAreStoredAsNfdByLcm()
    {
        // LocalMediaAdapter.BuildPathsDictionary prefers NFD when collapsing twins;
        // this test proves LCM also only ever serves audio refs as NFD, so the two align.
        const string nfc = "süülda.wav";
        const string nfd = "süülda.wav";
        nfc.Should().Be(nfc.Normalize(NormalizationForm.FormC));
        nfd.Should().Be(nfd.Normalize(NormalizationForm.FormD));
        nfc.Should().NotBe(nfd, "test is vacuous if the two literals are equal");

        var entryId = await AddFileDirectly(nfc, contents: "test");

        // Set via LCM with the NFC form; LCM serves back the NFD form.
        GetFwAudioValue(entryId).Should().Be(nfd);
    }

    [Fact]
    public async Task CanOpenAFile()
    {
        var fileName = "CanOpenAFile.txt";
        var entryId = await AddFileDirectly(fileName, "test");

        var entry = await _api.GetEntry(entryId);

        entry.Should().NotBeNull();
        var file = await _api.GetFileStream(new MediaUri(entry.CitationForm[_audioWs]));
        await using var stream = file.Stream;
        stream.Should().NotBeNull();
        using var streamReader = new StreamReader(stream);
        var contents = await streamReader.ReadToEndAsync();
        contents.Should().Be("test");
    }

    [Fact]
    public async Task GetEntry_MissingFileWorks()
    {
        var fileName = "GetEntry_MissingFileWorks.txt";
        var entryId = await AddFileDirectly(fileName, "test", storeFile: false);
        File.Exists(Path.Combine(_api.Cache.LangProject.LinkedFilesRootDir,
            FwDataMiniLcmApi.AudioVisualFolder,
            fileName)).Should().BeFalse();

        var entry = await _api.GetEntry(entryId);

        entry.Should().NotBeNull();
        entry.CitationForm[_audioWs].Should().Be(MediaUri.NotFound.ToString());
        GetFwAudioValue(entryId).Should().Be(fileName);
    }

    [Fact]
    public async Task UpdateEntry_MissingFileDoesNotOverwriteFwData()
    {
        var fileName = "UpdateEntry_MissingFileDoesNotOverwriteFwData.txt";
        var entryId = await AddFileDirectly(fileName, "test", storeFile: false);
        File.Exists(Path.Combine(_api.Cache.LangProject.LinkedFilesRootDir,
            FwDataMiniLcmApi.AudioVisualFolder,
            fileName)).Should().BeFalse();

        var entry = await _api.GetEntry(entryId);
        entry.Should().NotBeNull();
        await _api.UpdateEntry(entryId,
            new UpdateObjectInput<Entry>().Set(e => e.CitationForm[_audioWs], MediaUri.NotFound.ToString()));

        var fwAudioValue = GetFwAudioValue(entry.Id);
        fwAudioValue.Should().Be(fileName);
    }

    [Fact]
    public async Task GetEntry_RootedPathUnderLinkedFilesResolvesNormally()
    {
        // Decided handling (ticket 13, normalize-then-classify): a rooted/absolute audio path that
        // resolves UNDER LinkedFilesRootDir/AudioVisual is a managed file expressed as an absolute
        // path. It must be relativized and resolved normally -> a real, resolvable MediaUri.
        // TODAY this instead crashes: ToMediaUri (FwDataMiniLcmApi.cs:896-897) throws
        // ArgumentException("Media path must be relative") on the FwData->CRDT read, before any
        // adapter/DB call, so this test is RED (it throws inside GetEntry) until the fix lands.
        var fileName = "GetEntry_RootedPathUnderLinkedFiles.txt";
        var fileId = await StoreFileContentsAsync(fileName, "test");
        var rootedPathUnderTree = Path.Combine(_api.Cache.LangProject.LinkedFilesRootDir,
            FwDataMiniLcmApi.AudioVisualFolder, fileName);
        Path.IsPathRooted(rootedPathUnderTree).Should().BeTrue("the test is vacuous unless the stored FwData value is a rooted path");
        var entryId = await AddFileDirectly(rootedPathUnderTree, contents: null, storeFile: false);
        // guard: FwData really holds the rooted absolute path, not a relative one
        GetFwAudioValue(entryId).Should().Be(rootedPathUnderTree);

        var entry = await _api.GetEntry(entryId);

        entry.Should().NotBeNull();
        entry.CitationForm[_audioWs].Should().Be(new MediaUri(fileId, "localhost").ToString(),
            "a rooted path under LinkedFilesRootDir/AudioVisual should be relativized and resolved to its managed MediaUri; today ToMediaUri instead throws ArgumentException(\"Media path must be relative\") on read (FwDataMiniLcmApi.cs:896-897)");
    }

    [Fact]
    public async Task GetEntry_OutOfTreeRootedPathMapsToNotFoundSentinel()
    {
        // Decided handling (ticket 13): a GENUINELY out-of-tree rooted path can't be resolved to a
        // managed media file, so on the FwData->CRDT read it must become the not-found sentinel
        // (no throw) - the same value the bare-filename missing-file case already produces
        // (GetEntry_MissingFileWorks). It then SKIPS on the CRDT->FwData write (ShouldSet guards
        // MediaUri.NotFoundString), leaving FwData's original reference untouched.
        // TODAY this crashes instead: ToMediaUri (FwDataMiniLcmApi.cs:896-897) throws
        // ArgumentException("Media path must be relative") on read, so this test is RED (it throws
        // inside GetEntry) until the fix lands.
        var outOfTreeRootedPath = Path.Combine(Path.GetTempPath(),
            "lexbox-out-of-tree-media", "GetEntry_OutOfTreeRootedPath.wav");
        Path.IsPathRooted(outOfTreeRootedPath).Should().BeTrue("the test is vacuous unless the stored FwData value is a rooted path");
        outOfTreeRootedPath.StartsWith(_api.Cache.LangProject.LinkedFilesRootDir).Should()
            .BeFalse("the test is vacuous unless the rooted path is genuinely outside LinkedFilesRootDir");
        var entryId = await AddFileDirectly(outOfTreeRootedPath, contents: null, storeFile: false);
        // guard: FwData really holds the rooted out-of-tree path
        GetFwAudioValue(entryId).Should().Be(outOfTreeRootedPath);

        // read path: out-of-tree rooted path -> not-found sentinel, no throw
        var entry = await _api.GetEntry(entryId);
        entry.Should().NotBeNull();
        entry.CitationForm[_audioWs].Should().Be(MediaUri.NotFound.ToString(),
            "a genuinely out-of-tree rooted path should map to the not-found sentinel on read; today ToMediaUri instead throws ArgumentException(\"Media path must be relative\") (FwDataMiniLcmApi.cs:896-897)");

        // write path: the sentinel skips (ShouldSet), so FwData keeps its original out-of-tree reference
        await _api.UpdateEntry(entryId,
            new UpdateObjectInput<Entry>().Set(e => e.CitationForm[_audioWs], MediaUri.NotFound.ToString()));
        GetFwAudioValue(entryId).Should().Be(outOfTreeRootedPath,
            "writing the not-found sentinel must skip the audio field and leave FwData's original reference untouched");
    }

    [Fact]
    public async Task UpdateEntry_ClearAudioViaRemove_ClearsFwData()
    {
        var fileName = "ClearAudioViaRemove.wav";
        var fileId = await StoreFileContentsAsync(fileName, "test");
        var mediaUri = new MediaUri(fileId, "localhost");
        var entry = await _api.CreateEntry(new Entry
        {
            LexemeForm = { ["en"] = "test" }, CitationForm = { [_audioWs] = mediaUri.ToString() }
        });
        GetFwAudioValue(entry.Id).Should().Be(fileName);

        var before = (await _api.GetEntry(entry.Id))!;
        var after = before.Copy();
        after.CitationForm.Remove(_audioWs);
        await _api.UpdateEntry(before, after);

        var cleared = await _api.GetEntry(entry.Id);
        cleared!.CitationForm.Values.Should().NotContainKey(_audioWs, "removing the audio WS must clear it in FwData");
    }

    [Fact]
    public async Task UpdateEntry_ClearAudioViaEmptyString_ClearsFwData()
    {
        var fileName = "ClearAudioViaEmpty.wav";
        var fileId = await StoreFileContentsAsync(fileName, "test");
        var mediaUri = new MediaUri(fileId, "localhost");
        var entry = await _api.CreateEntry(new Entry
        {
            LexemeForm = { ["en"] = "test" }, CitationForm = { [_audioWs] = mediaUri.ToString() }
        });
        GetFwAudioValue(entry.Id).Should().Be(fileName);

        await _api.UpdateEntry(entry.Id,
            new UpdateObjectInput<Entry>().Set(e => e.CitationForm[_audioWs], ""));

        var cleared = await _api.GetEntry(entry.Id);
        cleared!.CitationForm.Values.Should().NotContainKey(_audioWs, "setting the audio WS to empty must clear it in FwData");
    }

    [Fact]
    public async Task SearchEntries_DoesNotMatchAudioWritingSystemValues()
    {
        // The audio writing system's value is a media-file reference, not searchable text.
        _audioWs.IsAudio.Should().BeTrue("the whole test is vacuous unless this is an audio writing system");
        var fileName = "audioonlysearchtoken.wav";
        var entryId = await AddFileDirectly(fileName, "test");

        // guard against a vacuous test: the entry is findable via its real (non-audio) lexeme form
        (await _api.SearchEntries("test").ToArrayAsync()).Should().Contain(e => e.Id == entryId);

        // the token only appears in the audio media reference, so it must not match a text search
        (await _api.SearchEntries("audioonlysearchtoken").ToArrayAsync()).Should().NotContain(e => e.Id == entryId);
    }

    [Fact]
    public async Task GetStreamForNotFoundIsNull()
    {
        var fileStream = await _api.GetFileStream(MediaUri.NotFound);
        fileStream.Stream.Should().BeNull();
        fileStream.Result.Should().Be(ReadFileResult.NotFound);
    }
}
