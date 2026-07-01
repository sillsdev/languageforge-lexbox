namespace FwLiteWeb.Hubs;

public interface ILexboxHubClient
{
    Task OnEntriesChanged(Guid[] changedEntryIds, Guid[] deletedEntryIds);
    Task OnProjectClosed(CloseReason reason);
}

public enum CloseReason
{
    User,
    Locked
}
