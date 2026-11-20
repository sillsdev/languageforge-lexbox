namespace FwLiteShared.Auth;

public interface ITokenCacheStore
{
    /// <returns>null when nothing has been saved yet</returns>
    byte[]? Read();
    void Write(byte[] data);
}
