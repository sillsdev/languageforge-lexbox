namespace FwDataMiniLcmBridge.Api;

public class VersionInvalidException(string type, Exception? innerException = null) : Exception(
    $"version of {type} is invalid, it has been changed since this version was fetched",
    innerException);
