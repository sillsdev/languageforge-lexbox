using System.Text.RegularExpressions;
using LexCore.Exceptions;

namespace LexBoxApi.GraphQL;

public static class SimpleValidator
{
    public static void Email(string email)
    {
        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250)))
        {
            throw InvalidFormatException.Email();
        }
    }
}
