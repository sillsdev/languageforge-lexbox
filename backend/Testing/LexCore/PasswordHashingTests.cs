using LexCore;
using Shouldly;

namespace Testing.LexCore;

public class PasswordHashingTests
{
    [InlineData("h0vJVjQUOfxaEC3Uddz", "266d7346dbf074a28ab29ef730191feb", "ba205a1131e34030fa1507a77cb9d1bd0c5fb800")]
    [InlineData("5!2j6cwau2ZqUXL", "57b7216f93b66d8dfa6cc0dad910025d", "f17f1754dfcf346298e2154071fc077203900a28")]
    [Theory]
    public void CanHashPassword(string pw, string salt, string hash)
    {
        PasswordHashing.RedminePasswordHash(pw, salt, false).ShouldBe(hash);
    }
}