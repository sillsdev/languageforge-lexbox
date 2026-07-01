using System.Text;
using SystemTextJsonPatch;

namespace MiniLcm;

public static class JsonPatchExtensions
{
    public static string Summarize<T>(this UpdateObjectInput<T> update) where T : class
    {
        return Summarize(update.Patch);
    }

    /// <summary>
    /// Reads the value a patch sets for <paramref name="propertyName"/>. Returns false when the patch doesn't
    /// touch the property; throws when it does but the value isn't a <typeparamref name="TValue"/> (a malformed
    /// patch we'd rather fail loud on than silently ignore).
    /// </summary>
    public static bool TryGetPropertyChange<T, TValue>(this UpdateObjectInput<T> update, string propertyName, out TValue value) where T : class
    {
        value = default!;
        foreach (var op in update.Patch.Operations)
        {
            if (!string.Equals(op.Path, $"/{propertyName}", StringComparison.OrdinalIgnoreCase))
                continue;
            if (op.Value is TValue typed)
            {
                value = typed;
                return true;
            }
            throw new InvalidOperationException($"Unsupported value for the {propertyName} patch: '{op.Value}'. Expected {typeof(TValue).Name}.");
        }
        return false;
    }
    public static string Summarize<T>(this JsonPatchDocument<T> document) where T : class
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Update: {typeof(T).Name}");
        foreach (var op in document.Operations)
        {
            sb.AppendLine($"{op.OperationType} {op.Path}: {op.Value}");
        }
        return sb.ToString();
    }
}
