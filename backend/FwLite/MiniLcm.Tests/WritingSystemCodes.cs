using SIL.WritingSystems;

namespace MiniLcm.Tests;

public static class WritingSystemCodes
{
    public static string[] ValidTwoLetterCodes = StandardSubtags.RegisteredLanguages.Select(lang => lang.Code).ToArray();
}
