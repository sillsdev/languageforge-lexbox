using FwHeadless.Media;
using MiniLcm.Media;

namespace Testing.FwHeadless;

public class PendingReservationSubfolderTests
{
    [Theory]
    [InlineData("text/html", "Plugins", "Plugins")]
    [InlineData("text/html", null, "")]
    [InlineData("audio/wav", null, "AudioVisual")]
    [InlineData("image/png", null, "Pictures")]
    [InlineData("audio/wav", "../escape", "AudioVisual")]
    public void UsesTheDeclaredSubfolderThenTheMimeTypeGuess(string mimeType, string? subfolder, string expected)
    {
        var metadata = new LcmFileMetadata("file", mimeType, LinkedFilesSubfolder: subfolder);
        MediaFileService.PendingReservationSubfolder(metadata).Should().Be(expected);
    }
}
