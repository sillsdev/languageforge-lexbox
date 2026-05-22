using LcmCrdt.Project;
using MiniLcm.Models;

namespace LcmCrdt.Tests.Project;

public class ProjectTemplateAbbreviationTests
{
    [Theory]
    [InlineData("en", "En")]
    [InlineData("en-US", "En")]
    [InlineData("fr", "Fr")]
    [InlineData("qaa-Zxxx-x-audio", "Qaa")]
    [InlineData("fr-Latn-fonipa", "ipa")]
    public void AbbreviationFor_ProducesReasonableAbbreviation(string wsId, string expected)
    {
        ProjectTemplate.AbbreviationFor(new WritingSystemId(wsId)).Should().Be(expected);
    }
}
