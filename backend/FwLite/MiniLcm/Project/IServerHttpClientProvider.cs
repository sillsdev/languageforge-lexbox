namespace MiniLcm.Project;

/// <summary>
/// Provides an http client for the server related to a specific project.
/// </summary>
public interface IServerHttpClientProvider
{
    ValueTask<HttpClient> GetHttpClient();
    ValueTask<ConnectionStatus> ConnectionStatus(bool forceRefresh = false);
}

public enum ConnectionStatus
{
    Unknown,
    Online,
    Offline,
    NoServer,
    NotLoggedIn,
}
