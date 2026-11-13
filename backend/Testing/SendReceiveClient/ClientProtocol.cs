namespace SendReceiveClient;

public record ClientRequest(
    string Action = "",
    Dictionary<string, string>? Options = null,
    string? Username = null,
    string? Password = null,
    string HgArgs = "",
    string Dir = "");

public record ClientResponse(string Output, string? ExceptionType = null, string? Exception = null);
