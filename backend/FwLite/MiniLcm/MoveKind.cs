namespace MiniLcm;

public enum MoveKind
{
    /// <summary>Within the given parent only. Move throws if the child is under another parent; the CRDT Submit variant skips instead, since the other side moved or deleted the child.</summary>
    Reorder,
    Reparent,
}
