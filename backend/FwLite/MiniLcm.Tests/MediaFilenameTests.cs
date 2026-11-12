using MiniLcm.Media;

namespace MiniLcm.Tests;

public class MediaFilenameTests
{
    public static TheoryData<string?> InvalidNames =>
    [
        null,
        "",
        ".",
        "..",
        "../x.jpg",
        "dir/x.jpg",
        "dir\\x.jpg",
        "/x.jpg",
        "C:x.jpg",
        "x\n.jpg",
        "x\0.jpg",
        "what?.jpg",
        "a*b.jpg",
        "a<b>.jpg",
        "a|b.jpg",
        "say \"hi\".jpg",
        "x.jpg ",
        "x.jpg.",
        "con",
        "con.jpg",
        "NUL",
        "Lpt1.tar.gz",
    ];

    [Theory]
    [InlineData("photo.jpg")]
    [InlineData("my photo (1).jpg")]
    [InlineData("..hidden.wav")]
    [InlineData(" leading space.jpg")]
    [InlineData("ünïcode.mp3")]
    [InlineData("ሰላም.jpg")]
    [InlineData("console.wav")]
    [InlineData("com10.wav")]
    [InlineData("noextension")]
    public void IsValid_AcceptsBareNames(string filename)
    {
        MediaFilename.IsValid(filename).Should().BeTrue();
    }

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public void IsValid_RejectsPathsAndNamesWindowsCannotStore(string? filename)
    {
        MediaFilename.IsValid(filename).Should().BeFalse();
    }
}
