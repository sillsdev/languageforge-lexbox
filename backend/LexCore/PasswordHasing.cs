using System.Security.Cryptography;
using System.Text;

namespace LexCore;

public static class PasswordHashing
{
    public static bool IsValidPassword(string password, string salt, string hash, bool preHashedPassword)
    {
        return HashPassword(password, salt, preHashedPassword) == hash;
    }

    public static string HashPassword(string password, string salt, bool preHashedPassword)
    {
        return RedminePasswordHash(password, salt, preHashedPassword);
    }

    public static string RedminePasswordHash(string password, string salt, bool preHashedPassword)
    {
        var passwordHash = preHashedPassword ? password : RubySha1Hash(password);
        return RubySha1Hash(salt + passwordHash);
    }

    //this is a re-implementation of the sha1 hexdigest method in ruby
    //https: //ruby-doc.org/stdlib-2.4.0/libdoc/digest/rdoc/Digest/SHA1.html
    private static string RubySha1Hash(string text)
    {
        return Convert.ToHexString(
            SHA1.HashData(
                Encoding.Default.GetBytes(text)
            )
        ).ToLower();
    }
}