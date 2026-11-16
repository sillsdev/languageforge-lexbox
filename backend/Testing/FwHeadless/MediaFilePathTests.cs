using FwHeadless.Media;

namespace Testing.FwHeadless;

public class MediaFilePathTests
{
    [Fact]
    public void ForUpload_StoresTheFilenameAsNfd()
    {
        var fileId = Guid.NewGuid();
        MediaFilePath.ForUpload("Pictures", fileId, "süülda.jpg")
            .Should().Be($"LinkedFiles/Pictures/{fileId}/süülda.jpg");
    }
}
