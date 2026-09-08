namespace MiniLcm.Exceptions;

/// <summary>
/// We only support reparenting some entity types.
/// </summary>
public class MoveNotSupportedException : NotSupportedException
{
    public MoveNotSupportedException(string typeName, object id, object beforeParentId, object afterParentId)
        : base($"{typeName} {id} was moved from parent {beforeParentId} to {afterParentId}; {Unsupported(typeName)}")
    {
    }

    /// <summary>For the diff walk, which knows the item arrived from elsewhere but not from where.</summary>
    public MoveNotSupportedException(string typeName, object id)
        : base($"{typeName} {id} was moved to a different parent; {Unsupported(typeName)}")
    {
    }

    private static string Unsupported(string typeName) => $"syncing {typeName} moves is not supported";
}
