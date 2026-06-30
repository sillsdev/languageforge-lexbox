using LcmCrdt.Project;
using MiniLcm.Models;

namespace LcmCrdt.Tests.Project;

public class ProjectTemplateAbbreviationTests
{
    [Theory]
    [InlineData("en", "Eng")]
    [InlineData("en-US", "Eng")]
    [InlineData("fr", "Fra")]
    [InlineData("qaa-Zxxx-x-audio", "Qaa")] // unlisted: no Iso3Code, fall back to subtag
    [InlineData("fr-Latn-fonipa", "ipa")]
    public void AbbreviationFor_ProducesReasonableAbbreviation(string wsId, string expected)
    {
        ProjectTemplate.AbbreviationFor(new WritingSystemId(wsId)).Should().Be(expected);
    }
}
