using System.Diagnostics.CodeAnalysis;
using MiniLcm.Models;

namespace MiniLcm.Exceptions;

public class NotFoundException(string identifier, string type) : Exception($"Not found: {identifier} ({type})")
{
    public NotFoundException(Guid id, Type type) : this(id.ToString(), type.Name)
    {
    }

    public static void ThrowIfNull<T>([AllowNull, NotNull] object arg, string identifier)
    {
        if (arg is null)
            throw ForType<T>(identifier);
    }

    public static void ThrowIfNull<T>([AllowNull, NotNull] object arg, Guid identifier)
    {
        ThrowIfNull<T>(arg, identifier.ToString());
    }

    public static NotFoundException ForType<T>(string identifier)
    {
        return new(identifier, typeof(T).Name);
    }

    public static NotFoundException ForType<T>(Guid identifier)
    {
        return ForType<T>(identifier.ToString());
    }

    public static NotFoundException ForWs(WritingSystemId id, WritingSystemType type)
    {
        return new($"{id.Code}: {type}", nameof(WritingSystem));
    }

    public static NotFoundException ForWs(WritingSystem ws)
    {
        return ForWs(ws.WsId.Code, ws.Type);
    }

    public string Type { get; set; } = type;
}
