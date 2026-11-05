using FwHeadless.Controllers;

namespace Testing.FwHeadless;

public class GuessSubfolderFromMimeTypeTests
{
    [Theory]
    [InlineData("image/png", "Pictures")]
    [InlineData("audio/wav", "AudioVisual")]
    [InlineData("video/mp4", "AudioVisual")]
    [InlineData("text/html", "Plugins")]
    [InlineData("text/html; charset=utf-8", "Plugins")]
    [InlineData("application/zip", null)]
    public void PicksTheLinkedFilesSubfolder(string mimeType, string? expected)
    {
        MediaFileController.GuessSubfolderFromMimeType(mimeType).Should().Be(expected);
    }
}
