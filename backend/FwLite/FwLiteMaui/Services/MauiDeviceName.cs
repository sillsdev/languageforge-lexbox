using FwLiteShared.LocalSync.Hosting;

namespace FwLiteMaui.Services;

public class MauiDeviceName : IDeviceName
{
    public string Name => DeviceInfo.Current.Name;
}
