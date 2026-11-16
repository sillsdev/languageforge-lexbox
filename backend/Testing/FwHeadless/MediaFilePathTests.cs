using FwHeadless.Media;

namespace Testing.FwHeadless;

public class MediaFilePathTests
{
    [Fact]
    public void ForUpload_StoresTheFilenameAsNfc()
    {
        var fileId = Guid.NewGuid();
        MediaFilePath.ForUpload("Pictures", fileId, "su\u0308u\u0308lda.jpg")
            .Should().Be($"LinkedFiles/Pictures/{fileId}/s\u00fc\u00fclda.jpg");
    }
}
