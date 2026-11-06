namespace MiniLcm.Exceptions;

/// <summary>
/// We only support reparenting some entity types.
/// </summary>
public class MoveNotSupportedException(string typeName, object id, object beforeParentId, object afterParentId) : NotSupportedException(
    $"{typeName} {id} was moved from parent {beforeParentId} to {afterParentId}; syncing {typeName} moves is not supported")
{ }
