using FwHeadless.Media;

namespace Testing.FwHeadless;

public class MediaFilePathTests
{
    [Theory]
    [InlineData("Pictures")]
    [InlineData("AudioVisual")]
    [InlineData("my-folder_2")]
    public void IsValidSubfolder_AcceptsPlainSegment(string subfolder)
    {
        MediaFilePath.IsValidSubfolder(subfolder).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("../x")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("/abs")]
    [InlineData("spaced folder")]
    public void IsValidSubfolder_RejectsAnythingElse(string? subfolder)
    {
        MediaFilePath.IsValidSubfolder(subfolder).Should().BeFalse();
    }

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
