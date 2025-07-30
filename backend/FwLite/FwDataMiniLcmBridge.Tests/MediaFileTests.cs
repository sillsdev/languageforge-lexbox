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
    public async Task GetStreamForNotFoundIsNull()
    {
        var fileStream = await _api.GetFileStream(MediaUri.NotFound);
        fileStream.Stream.Should().BeNull();
        fileStream.Result.Should().Be(ReadFileResult.NotFound);
    }
}
