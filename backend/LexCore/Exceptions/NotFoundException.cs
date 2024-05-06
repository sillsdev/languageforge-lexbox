using System.Diagnostics.CodeAnalysis;

namespace LexCore.Exceptions;

public class NotFoundException(string message, string type) : Exception(message)
{
    public static void ThrowIfNull<T>([AllowNull, NotNull] T arg)
    {
        if (arg is null)
            throw ForType<T>();
    }

    public static NotFoundException ForType<T>() => new($"{typeof(T).Name} not found", typeof(T).Name);

    public string Type { get; set; } = type;
}
