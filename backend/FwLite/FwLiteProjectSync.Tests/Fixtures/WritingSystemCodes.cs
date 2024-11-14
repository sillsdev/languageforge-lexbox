using SIL.WritingSystems;

namespace FwLiteProjectSync.Tests;

public static class WritingSystemCodes
{
    public static string[] ValidTwoLetterCodes = StandardSubtags.RegisteredLanguages.Select(lang => lang.Code).ToArray();
}
