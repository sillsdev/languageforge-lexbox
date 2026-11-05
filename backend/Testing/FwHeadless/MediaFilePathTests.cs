using FwHeadless.Media;

namespace Testing.FwHeadless;

public class MediaFilePathTests
{
    [Theory]
    [InlineData("photo.jpg")]
    [InlineData("my photo (1).jpg")]
    [InlineData("..hidden.wav")]
    [InlineData("ünïcode.mp3")]
    public void IsValidFilename_AcceptsBareName(string filename)
    {
        MediaFilePath.IsValidFilename(filename).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("../x.jpg")]
    [InlineData("dir/x.jpg")]
    [InlineData("dir\\x.jpg")]
    [InlineData("/x.jpg")]
    [InlineData("x\n.jpg")]
    public void IsValidFilename_RejectsPathsAndOddNames(string? filename)
    {
        MediaFilePath.IsValidFilename(filename).Should().BeFalse();
    }
}
