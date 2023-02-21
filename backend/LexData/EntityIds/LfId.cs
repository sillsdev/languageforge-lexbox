using System.Reflection;
using System.Runtime.Serialization;

namespace LexData.EntityIds;


public interface LfId
{
    Guid GetIdForDb();
    string GetIdForJson();

    public static bool IsTypeLfId(Type type)
    {
        return type.IsAssignableTo(typeof(LfId));
    }

    public static Type GetEntityType(Type idType)
    {
        if (!IsTypeLfId(idType)) throw new ArgumentException("type is not an lf id");
        return idType.GenericTypeArguments[0];
    }

    protected static string GetPrefixFromEntityType(Type type)
    {
        return type.Name + ":";
    }

    private static string FormatId(Guid id, Type entityType)
    {
        return $"{GetPrefixFromEntityType(entityType)}{id:N}";
    }

    public static LfId<T> FromDb<T>(Guid id)
    {
        return new LfId<T>(FormatId(id, typeof(T)));
    }

    private static void EnsurePrefixed(string idString, string prefix)
    {
        if (!IsPrefixed(idString, prefix))
        {
            throw new SerializationException($"""invalid id from json, id "{idString}" is not a {prefix[..^1]} id """);
        }
    }

    protected static bool IsPrefixed(string idString, string prefix)
    {
        return idString.StartsWith(prefix);
    }

    public static LfId FromJson(string idString, Type idType)
    {
        var prefix =  GetPrefixFromEntityType(GetEntityType(idType));
        EnsurePrefixed(idString, prefix);

        var id = Activator.CreateInstance(idType,
                         BindingFlags.NonPublic | BindingFlags.CreateInstance | BindingFlags.Instance,
                         null,
                         new[] { idString },
                         null)
                     as LfId ??
                 throw new SerializationException($"unable to create LfId from type {idType} with id {idString}");
        return id;
    }

    public static LfId<T> FromFrontend<T>(string idString)
    {
        var prefix = GetPrefixFromEntityType(typeof(T));
        EnsurePrefixed(idString, prefix);
        return new LfId<T>(idString);
    }
}


public readonly struct LfId<T> : LfId, IParsable<LfId<T>>
{
    public static readonly LfId<T> Empty = new();

    internal LfId(string id)
    {
        _id = id ?? throw new ArgumentNullException(nameof(id));
    }

    private readonly string _id;

    public Guid GetIdForDb()
    {
        if (_id == null)
        {
            throw new NullReferenceException(
                "LfId is empty, it should not be null, use a nullable if an id can be null");
        }

        var prefixLength = LfId.GetPrefixFromEntityType(typeof(T)).Length;
        return Guid.Parse(_id[prefixLength..]);
    }

    public string GetIdForJson()
    {
        return _id ?? throw new NullReferenceException(
            "LfId is empty, it should not be null, use a nullable if an id can be null");
    }

    public bool Equals(LfId<T> other)
    {
        return _id == other._id;
    }

    public override string ToString()
    {
        return _id ?? "{Empty Id}";
    }

    public override bool Equals(object? obj)
    {
        return obj is LfId<T> other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _id.GetHashCode();
    }

    public static bool operator ==(LfId<T> left, LfId<T> right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LfId<T> left, LfId<T> right)
    {
        return !left.Equals(right);
    }

    public static LfId<T> Parse(string s, IFormatProvider? provider = null)
    {
        return LfId.FromFrontend<T>(s);
    }

    public static bool TryParse(string? s, IFormatProvider? provider, out LfId<T> result)
    {
        if (s == null)
        {
            result = Empty;
            return false;
        }

        if (!LfId.IsPrefixed(s, LfId.GetPrefixFromEntityType(typeof(T))))
        {
            result = Empty;
            return false;
        }

        result = LfId.FromFrontend<T>(s);
        return true;
    }
}
