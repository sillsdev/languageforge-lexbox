using LexCore.Entities;
using FluentAssertions;

namespace Testing.LexCore;

public class InitFwDataProjectInputTests
{
    private static string? Validate(params string[] tags) =>
        InitFwDataProjectInput.ValidateWritingSystemList("vernacular", tags);

    // A valid BCP 47 tag of the given length. Private-use subtags are at most 8 characters each, and
    // libpalaso also rejects a repeated one, so each is different.
    private static string ValidTagOfLength(int length)
    {
        var tag = "qaa-x";
        var i = 0;
        while (tag.Length + 9 <= length) tag += $"-p{i++:D7}";
        if (tag.Length < length) tag += "-" + $"q{i:D7}"[..(length - tag.Length - 1)];
        return tag;
    }

    [Fact]
    public void Accepts_a_typical_list()
    {
        Validate("fr", "en-US", "qaa-Latn-x-mycustom").Should().BeNull();
    }

    [Fact]
    public void Rejects_a_null_list()
    {
        InitFwDataProjectInput.ValidateWritingSystemList("vernacular", null)
            .Should().Contain("At least one vernacular writing system is required");
    }

    [Fact]
    public void Rejects_an_empty_list()
    {
        Validate().Should().Contain("At least one vernacular writing system is required");
    }

    [Fact]
    public void Accepts_the_maximum_number_of_writing_systems()
    {
        var tags = Enumerable.Range(0, InitFwDataProjectInput.MaxWritingSystemsPerList).Select(i => $"qaa-x-ws{i}").ToArray();
        Validate(tags).Should().BeNull();
    }

    [Fact]
    public void Rejects_more_than_the_maximum_number_of_writing_systems()
    {
        var tags = Enumerable.Range(0, InitFwDataProjectInput.MaxWritingSystemsPerList + 1).Select(i => $"qaa-x-ws{i}").ToArray();
        Validate(tags).Should().Contain($"At most {InitFwDataProjectInput.MaxWritingSystemsPerList}");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Rejects_an_empty_tag(string tag)
    {
        Validate("fr", tag).Should().Contain("Empty vernacular writing system code");
    }

    [Fact]
    public void Accepts_a_tag_of_the_maximum_length()
    {
        var tag = ValidTagOfLength(InitFwDataProjectInput.MaxWritingSystemTagLength);
        tag.Length.Should().Be(InitFwDataProjectInput.MaxWritingSystemTagLength);
        Validate(tag).Should().BeNull();
    }

    [Fact]
    public void Rejects_a_tag_longer_than_the_maximum_and_shows_its_start()
    {
        var tag = "qaa-x-" + new string('a', InitFwDataProjectInput.MaxWritingSystemTagLength);
        var error = Validate("fr", tag);
        error.Should().Contain($"longer than {InitFwDataProjectInput.MaxWritingSystemTagLength} characters");
        error.Should().Contain(tag[..32]).And.NotContain(tag[..33]);
    }

    [Theory]
    [InlineData("not_a_tag")]
    [InlineData("qaa-x-thissubtagistoolong")]
    public void Rejects_a_tag_that_is_not_valid_BCP_47(string tag)
    {
        Validate("fr", tag).Should().Be($"Invalid vernacular writing system code: {tag}");
    }

    [Theory]
    [InlineData("fr", "fr")]
    [InlineData("fr", "FR")] // BCP 47 tags are case-insensitive
    public void Rejects_a_duplicate_tag(string first, string second)
    {
        Validate(first, "en", second).Should().Contain($"Duplicate vernacular writing system: {first}");
    }

    [Fact]
    public void Accepts_a_typical_UI_writing_system()
    {
        InitFwDataProjectInput.ValidateUiWritingSystem("en").Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Rejects_a_missing_UI_writing_system(string? tag)
    {
        InitFwDataProjectInput.ValidateUiWritingSystem(tag).Should().Contain("A UI writing system is required");
    }

    [Fact]
    public void Accepts_a_UI_writing_system_of_the_maximum_length()
    {
        var tag = ValidTagOfLength(InitFwDataProjectInput.MaxWritingSystemTagLength);
        InitFwDataProjectInput.ValidateUiWritingSystem(tag).Should().BeNull();
    }

    [Fact]
    public void Rejects_a_UI_writing_system_longer_than_the_maximum_and_shows_its_start()
    {
        var tag = "qaa-x-" + new string('a', InitFwDataProjectInput.MaxWritingSystemTagLength);
        var error = InitFwDataProjectInput.ValidateUiWritingSystem(tag);
        error.Should().Contain($"longer than {InitFwDataProjectInput.MaxWritingSystemTagLength} characters");
        error.Should().Contain(tag[..32]).And.NotContain(tag[..33]);
    }

    [Fact]
    public void Rejects_a_UI_writing_system_that_is_not_valid_BCP_47()
    {
        InitFwDataProjectInput.ValidateUiWritingSystem("not_a_tag").Should().Be("Invalid UI writing system code: not_a_tag");
    }
}
