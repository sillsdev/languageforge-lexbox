namespace MiniLcm.Exceptions;

/// <summary>The child exists, but not under the parent the caller named.</summary>
public class ParentMismatchException(string typeName, Guid id, Guid expectedParentId, Guid actualParentId)
    : Exception($"{typeName} {id} does not belong to the expected parent {expectedParentId}, its parent is {actualParentId}")
{
    public static ParentMismatchException ForType<T>(Guid id, Guid expectedParentId, Guid actualParentId)
    {
        return new(typeof(T).Name, id, expectedParentId, actualParentId);
    }
}
