using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.Media;
using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm.Models;
using SIL.LCModel.Infrastructure;

namespace FwDataMiniLcmBridge.Tests;

[Collection(ProjectLoaderFixture.Name)]
public class MediaFileTests : IAsyncLifetime
{
    private readonly FwDataMiniLcmApi _api;
    private readonly WritingSystemId _audioWs = "en-Zxxx-x-audio";

    public MediaFileTests(ProjectLoaderFixture fixture)
    {
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

    private async Task<Guid> AddFileDirectly(string fileName, string? contents)
    {
        await StoreFileContentsAsync(fileName, contents);

        var entry = await _api.CreateEntry(new Entry() { LexemeForm = { ["en"] = "test" } });
        var lexEntry = _api.EntriesRepository.GetObject(entry.Id);
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Set LexemeFormOA to null",
            "Restore LexemeFormOA",
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
        var fwFilePath = Path.Combine(FwDataMiniLcmApi.AudioVisualFolder, fileName);
        var filePath = Path.Combine(_api.Cache.LangProject.LinkedFilesRootDir, fwFilePath);
        await File.WriteAllTextAsync(filePath, contents);
        return LocalMediaAdapter.NewGuidV5(fwFilePath);
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
        var fileGuid = LocalMediaAdapter.NewGuidV5(Path.Combine(FwDataMiniLcmApi.AudioVisualFolder, fileName));
        var entryId = await AddFileDirectly(fileName, "test");

        var entry = await _api.GetEntry(entryId);

        entry.Should().NotBeNull();
        entry.CitationForm[_audioWs].Should().Be(new MediaUri(fileGuid, "localhost").ToString());
        GetFwAudioValue(entryId).Should().Be(fileName);
    }

    [Fact]
    public async Task CreateEntry_MapsMediaUrisForAudioWs()
    {
        var fileId = await StoreFileContentsAsync("CreateEntry_MapsMediaUrisForAudioWs.txt", "test");
        var mediaUri = new MediaUri(fileId, "localhost");
        var entry = await _api.CreateEntry(new Entry()
        {
            LexemeForm = { ["en"] = "test" },
            CitationForm = { [_audioWs] = mediaUri.ToString() }
        });

        var fwAudioValue = GetFwAudioValue(entry.Id);
        fwAudioValue.Should().Be("CreateEntry_MapsMediaUrisForAudioWs.txt");
        entry.CitationForm[_audioWs].Should().Be(mediaUri.ToString());
    }

    [Fact]
    public async Task CanOpenAFile()
    {
        var fileName = "CanOpenAFile.txt";
        var entryId = await AddFileDirectly(fileName, "test");

        var entry = await _api.GetEntry(entryId);

        entry.Should().NotBeNull();
        await using var file = await _api.GetFileStream(new MediaUri(entry.CitationForm[_audioWs]));
        file.Should().NotBeNull();
        using var streamReader = new StreamReader(file);
        var contents = await streamReader.ReadToEndAsync();
        contents.Should().Be("test");
    }
}
