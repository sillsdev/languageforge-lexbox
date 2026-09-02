namespace MiniLcm.Exceptions;

/// <summary>
/// A sync diff found an item under different parents in the before and after states — a move —
/// for an item type the sync can't move yet. Throwing beats applying the move as an unrelated
/// delete and create, which silently destroys the item on the CRDT side.
/// </summary>
public class MoveNotSupportedException(string typeName, object id)
    : NotSupportedException($"{typeName} {id} was moved to a different parent; syncing {typeName} moves is not supported")
{
}
