using System.Text;
using SystemTextJsonPatch;

namespace MiniLcm;

public static class JsonPatchExtensions
{
    public static string Summarize<T>(this UpdateObjectInput<T> update) where T : class
    {
        return Summarize(update.Patch);
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
