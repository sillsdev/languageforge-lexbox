# Platform API gotchas

Each line was checked against the code it names. Read the cited file before changing that area; first guesses here were wrong in past sessions.

## Windows (`Windows/AppUpdateService.cs`)

- `PackageManager.AddPackageByUriAsync`: never attach a `.Progress` handler. In .NET desktop apps it drops the WinRT completion, so the await never returns (shipped once as a "Downloading…" hang). Byte progress comes from the loopback proxy instead.
- `PackageDeploymentManager.IsPackageRegistrationPending` (Windows App SDK, `Microsoft.Windows.Management.Deployment`) takes the package **full name** (`Package.Current.Id.FullName`), not the family name.
- Restart onto a staged update: not `AppInstance.Restart` (relaunches the old exe, so registration never applies). Call `RegisterApplicationRestart`, then `RegisterPackageByFamilyNameAsync(..., DeploymentOptions.ForceTargetApplicationShutdown, ...)`; the engine kills the package and Restart Manager relaunches the new version.
- `-p:FwLiteFlavor=Dev` changes only the Android `ApplicationId` and title. The MSIX identity stays `FwLiteDesktop` (`Windows/Package.appxmanifest`); the update test harness uses its own identity (`backend/FwLite/testing/README.md`).
- The `.appinstaller` served by LexBox (`backend/LexBoxApi/Services/FwLiteReleases/FwLiteReleaseService.cs`) uses the `appinstaller/2021` schema namespace.
- Debug builds are unpackaged (`WindowsPackageType=None`), so `Package.Current` throws and the app runs as portable (`FwLiteMauiKernel.IsPortableApp`); MSIX-only code paths need an installed package to test.

## Android (`Android/AndroidInAppUpdateService.cs`)

- Play in-app update bindings live in `Xamarin.Google.Android.Play.Core.AppUpdate*` (package `Xamarin.Google.Android.Play.App.Update`).
- Write `Android.Gms.Tasks.IOnSuccessListener` / `IOnFailureListener` fully qualified; a `using Android.Gms.Tasks;` brings in a `Task` type that clashes with `System.Threading.Tasks.Task`.
