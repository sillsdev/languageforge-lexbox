using FwHeadless.Media;

namespace Testing.FwHeadless;

public class MediaFilePathTests
{
    [Theory]
    [InlineData("Pictures", true)]
    [InlineData("AudioVisual", true)]
    [InlineData("my-folder_2", true)]
    [InlineData("", false)]
    [InlineData(".", false)]
    [InlineData("..", false)]
    [InlineData("../escape", false)]
    [InlineData("a/b", false)]
    [InlineData("a\\b", false)]
    [InlineData("C:", false)]
    [InlineData("spaced folder", false)]
    public void IsValidSubfolder(string subfolder, bool expected)
    {
        MediaFilePath.IsValidSubfolder(subfolder).Should().Be(expected);
    }

    [Theory]
    [InlineData("word.wav", true)]
    [InlineData("a..b.txt", true)]
    [InlineData("spaced name.png", true)]
    [InlineData("ünïcode.jpg", true)]
    [InlineData("", false)]
    [InlineData(".", false)]
    [InlineData("..", false)]
    [InlineData("../escape.zip", false)]
    [InlineData("dir/file.zip", false)]
    [InlineData("dir\\file.zip", false)]
    [InlineData("bad\nname.txt", false)]
    public void IsValidFilename(string filename, bool expected)
    {
        MediaFilePath.IsValidFilename(filename).Should().Be(expected);
    }
}
