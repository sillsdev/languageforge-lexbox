using Windows.ApplicationModel;
using Microsoft.Extensions.Logging;

namespace FwLiteMaui;

/// <summary>
/// Logs Windows updating our own MSIX package. The OS stages an update on its own schedule and registers it
/// at activation, force-terminating this process if it's running, which otherwise leaves nothing in our log.
/// </summary>
public class PackageUpdateLogger(ILogger<PackageUpdateLogger> logger) : IMauiInitializeService
{
    //field, not a local: the subscription stops firing once the catalog is collected
    private PackageCatalog? _packageCatalog;
    private int _loggedProgressDecile = -1;

    public void Initialize(IServiceProvider services)
    {
        try
        {
            _packageCatalog = PackageCatalog.OpenForCurrentPackage();
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Unable to open the package catalog, OS package updates won't be logged");
            return;
        }

        _packageCatalog.PackageUpdating += OnPackageUpdating;
    }

    private void OnPackageUpdating(PackageCatalog sender, PackageUpdatingEventArgs args)
    {
        var version = FormatVersion(args.TargetPackage);
        if (args.ErrorCode is { HResult: not 0 })
        {
            logger.LogError(args.ErrorCode, "Windows failed to update this app to {TargetVersion}", version);
            return;
        }

        if (args.IsComplete)
        {
            logger.LogInformation("Windows finished updating this app to {TargetVersion}", version);
            return;
        }

        //progress fires often, so only log each 10% step
        var decile = (int)(args.Progress / 10);
        if (decile == _loggedProgressDecile) return;
        _loggedProgressDecile = decile;
        logger.LogInformation("Windows is updating this app to {TargetVersion}: {Progress}%", version, args.Progress);
    }

    private static string FormatVersion(Package? package)
    {
        if (package is null) return "unknown";
        var version = package.Id.Version;
        return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }
}
