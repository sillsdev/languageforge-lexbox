using System.Reflection;
using FwLiteShared.Services;

namespace FwLiteMaui;

public class AppVersion
{
    public static readonly string Version = VersionHelper.DisplayVersion(typeof(AppVersion).Assembly);
}
