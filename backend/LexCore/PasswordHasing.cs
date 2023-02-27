using System.Security.Cryptography;
using System.Text;

namespace LexCore;

public static class PasswordHashing
{
    public static string HashPassword(string password, string salt)
    {
        return RedminePasswordHash(password, salt);
    }
    
    public static string RedminePasswordHash(string password, string salt)
    {
        return RubySha1Hash(salt + RubySha1Hash(password));
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