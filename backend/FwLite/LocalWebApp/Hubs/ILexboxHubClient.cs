using MiniLcm.Models;

namespace LocalWebApp.Hubs;

public interface ILexboxHubClient
{
    Task OnEntryUpdated(Entry entry);
    Task OnProjectClosed(CloseReason reason);
}

public enum CloseReason
{
    User,
    Locked
}
