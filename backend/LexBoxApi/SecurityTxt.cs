namespace LexBoxApi;

public static class SecurityTxt
{
    public const string RawText = """
                                  Contact: https://github.com/sillsdev/languageforge-lexbox/security
                                  Expires: 2026-01-01T05:00:00.000Z
                                  Preferred-Languages: en
                                  """;

    public static void MapSecurityTxt(this IEndpointRouteBuilder builder)
    {
        builder.Map("/security.txt", () => RawText).AllowAnonymous();
        builder.Map("/.well-known/security.txt", () => RawText).AllowAnonymous();
    }
}
