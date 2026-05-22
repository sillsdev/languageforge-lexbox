using LcmCrdt.Project;
using MiniLcm.Models;

namespace LcmCrdt.Tests.Project;

public class ProjectTemplateAbbreviationTests
{
    [Theory]
    [InlineData("en", "Eng")]
    [InlineData("en-US", "Eng")]
    [InlineData("fr", "Fra")]
    [InlineData("qaa-x-foo", "Foo")]
    [InlineData("qaa-Zxxx-x-audio", "Au")]
    [InlineData("fr-Latn-fonipa", "Fra-Ipa")]
    [InlineData("xyz", "Xyz")]
    public void AbbreviationFor_ProducesReasonableAbbreviation(string wsId, string expected)
    {
        ProjectTemplate.AbbreviationFor(new WritingSystemId(wsId)).Should().Be(expected);
    }
}
