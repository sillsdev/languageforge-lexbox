using LexCore;
using Shouldly;

namespace Testing.LexCore;

public class PasswordHashingTests
{
    [InlineData("h0vJVjQUOfxaEC3Uddz", "266d7346dbf074a28ab29ef730191feb", "ba205a1131e34030fa1507a77cb9d1bd0c5fb800")]
    [Theory]
    public void CanHashPassword(string pw, string salt, string hash)
    {
        PasswordHashing.RedminePasswordHash(pw, salt).ShouldBe(hash);
    }
}