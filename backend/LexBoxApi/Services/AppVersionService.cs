using System.Reflection;
using LexCore;

namespace LexBoxApi.Services;

public static class AppVersionService
{
    public static readonly string Version = AppVersion.Get(typeof(AppVersionService));
}
