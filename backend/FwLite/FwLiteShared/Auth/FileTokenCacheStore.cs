namespace FwLiteShared.Auth;

/// <summary>
/// Saves the token cache unencrypted, readable only by the current user. For the Mac app: the file sits in its
/// sandbox container, and the keychain is off limits there (OSStatus -34018) without a keychain-access-groups
/// entitlement, which a Developer ID app only gets with a provisioning profile.
/// </summary>
public class FileTokenCacheStore(string path) : ITokenCacheStore
{
    public byte[]? Read() => File.Exists(path) ? File.ReadAllBytes(path) : null;

    public void Write(byte[] data)
    {
        var tempPath = path + ".tmp";
        var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write };
        if (!OperatingSystem.IsWindows()) options.UnixCreateMode = UnixFileMode.UserRead | UnixFileMode.UserWrite;
        using (var stream = new FileStream(tempPath, options))
        {
            stream.Write(data);
        }
        File.Move(tempPath, path, overwrite: true);
    }
}
