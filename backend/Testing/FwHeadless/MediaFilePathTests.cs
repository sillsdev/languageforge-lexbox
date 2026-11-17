using FwHeadless.Media;

namespace Testing.FwHeadless;

public class MediaFilePathTests
{
    // ü as one code point, and as u + combining diaeresis
    private const string Nfc = "\u00fc";
    private const string Nfd = "u\u0308";
    [Fact]
    public void ForUpload_StoresTheFilenameAsNfc()
    {
        var fileId = Guid.NewGuid();
        MediaFilePath.ForUpload("Pictures", fileId, $"{Nfd}.jpg")
            .Should().Be($"LinkedFiles/Pictures/{fileId}/{Nfc}.jpg");
    }
}
