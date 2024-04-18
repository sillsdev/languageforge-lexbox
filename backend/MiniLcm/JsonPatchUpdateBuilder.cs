using SystemTextJsonPatch;

namespace MiniLcm;

public class JsonPatchUpdateInput<T>(JsonPatchDocument<T> patchDocument) : UpdateObjectInput<T>
    where T : class
{
    public JsonPatchDocument<T> Patch { get; } = patchDocument;

    public void Apply(T obj)
    {
        Patch.ApplyTo(obj);
    }
}
