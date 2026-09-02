namespace MiniLcm.Exceptions;

/// <summary>
/// The sync found an item under different parents in the before and after states — a move —
/// for an item type the sync can't move yet. Throwing beats applying the move as an unrelated
/// delete and create, which silently destroys the item on the CRDT side.
/// </summary>
public class MoveNotSupportedException(string typeName, Guid id, Guid beforeParentId, Guid afterParentId)
    : NotSupportedException($"{typeName} {id} was moved from parent {beforeParentId} to {afterParentId}; syncing {typeName} moves is not supported")
{
}
