namespace FwLiteShared.LocalSync.Hosting;

/// <summary>What a joiner with no author name is called on the host's screen.</summary>
public interface IDeviceName
{
    string Name { get; }
}

// MAUI replaces it: on Android this is "localhost"
internal sealed class MachineDeviceName : IDeviceName
{
    public string Name => Environment.MachineName;
}
