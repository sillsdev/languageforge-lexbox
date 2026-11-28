# Platform API gotchas

Each line was checked against the code it names. Read the cited file before changing that area; first guesses here were wrong in past sessions.

## Windows (`Windows/AppUpdateService.cs`)

- `PackageManager.AddPackageByUriAsync`: never attach a `.Progress` handler. In .NET desktop apps it drops the WinRT completion, so the await never returns (shipped once as a "Downloading…" hang). Byte progress comes from the loopback proxy instead.
- `PackageDeploymentManager.IsPackageRegistrationPending` (Windows App SDK, `Microsoft.Windows.Management.Deployment`) takes the package **full name** (`Package.Current.Id.FullName`), not the family name.
- Restart onto a staged update: not `AppInstance.Restart` (relaunches the old exe, so registration never applies). Call `RegisterApplicationRestart`, then `RegisterPackageByFamilyNameAsync(..., DeploymentOptions.ForceTargetApplicationShutdown, ...)`; the engine kills the package and Restart Manager relaunches the new version.
- `-p:FwLiteFlavor=Dev` changes only the Android `ApplicationId` and title. The MSIX identity stays `FwLiteDesktop` (`Windows/Package.appxmanifest`); the update test harness uses its own identity (`backend/FwLite/testing/README.md`).
- Debug builds are unpackaged (`WindowsPackageType=None`), so `Package.Current` throws and the app runs as portable (`FwLiteMauiKernel.IsPortableApp`); MSIX-only code paths need an installed package to test.

## Windows startup splash (`Windows/StartupSplash.cs`, `Windows/SplashHandOff.cs`)

- MAUI's splash screen is Android/iOS only and the manifest `uap:SplashScreen` is ignored for WinUI 3 desktop apps, hence the native window started from `Windows/Program.cs`.
- The main window waits off screen and is revealed once the viewer reports its first screen through `lexbox.WhenFirstScreenReady` (`frontend/viewer/src/lib/first-screen.ts`). Revealing as soon as the data arrives shows a white window for 200 to 350 ms.
- A failure in the splash or the hand-off must never stop the app from opening; both fall back to a normal start. Env `FWLITE_NO_SPLASH=1` skips them, for a machine where they misbehave.
- Portable (single-file) builds show nothing for 10 to 20 s on the first run of a new exe: that wait is before .NET starts, so the splash can't cover it.

## Mac Catalyst (`MacCatalyst/MacUpdateService.cs`)

- Updates use Velopack, reading the GitHub releases directly (`GithubSource`); Lexbox's `should-update` still decides whether one is offered. Moving the feed onto Lexbox is #3077.
- Velopack only recognises Mac Catalyst from 1.3, a prerelease when this was written. The `Velopack` NuGet version (`Directory.Packages.props`) and `VPK_VERSION` (`fw-lite.yaml`) must match.
- `VelopackApp.Build().Run()` is the first call in `MacCatalyst/Program.cs`. It also applies an update downloaded before the last quit.
- Only a Velopack-packed bundle (it has `Contents/MacOS/UpdateMac`) can update itself; dev builds and pre-Velopack DMG installs report `SupportsAutoUpdate` false and get the Download prompt.
- `GithubSource` reads the 10 newest GitHub releases and skips any without `releases.osx.json`, such as the `lexbox-v…` releases.
- The package version is the semver `YYYY.M.D`. A second release on the same UTC day has the same version, so Macs already on the first don't update to it; see the release workaround in `.github/AGENTS.md`.

## Android (`Android/AndroidInAppUpdateService.cs`)

- Play in-app update bindings live in `Xamarin.Google.Android.Play.Core.AppUpdate*` (package `Xamarin.Google.Android.Play.App.Update`).
- Write `Android.Gms.Tasks.IOnSuccessListener` / `IOnFailureListener` fully qualified; a `using Android.Gms.Tasks;` brings in a `Task` type that clashes with `System.Threading.Tasks.Task`.
