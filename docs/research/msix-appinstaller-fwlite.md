# MSIX `.appinstaller` auto-update for FW Lite — research findings

> **Location note:** the repo had no `docs/research/` or `docs/adr/` convention, so this file
> establishes `docs/research/`. Move it if the team prefers a different home.
>
> **Scope:** originally research-only; **now updated (2026-07-29) with hands-on verification results
> and the implementation that landed on branch `fwlite-appinstaller`.** Grounded in first-party
> Microsoft Learn docs (MSIX / App Installer conceptual docs, the `.appinstaller` XML schema
> reference, and the `Windows.Management.Deployment` / `Windows.ApplicationModel` WinRT API pages),
> plus manual testing against a real `.appinstaller`.
>
> **Date:** researched 2026-07-28, verified + implemented 2026-07-29.

---

## Status: verified and implemented (2026-07-29)

Every make-or-break question was tested manually against real signed bundles hosted on a plain
nginx server, and the two prior **UNCERTAIN** items were both resolved **in favour of shipping**:

| Question | Result |
| --- | --- |
| OS auto-update works from a plain host with our signed bundle? | ✅ Confirmed (installed via `.appinstaller`, published a newer version, it updated). |
| **Retroactive attach** — can a *bare-bundle* install (how all current users are installed) be migrated onto the track? | ✅ **Confirmed.** Installing the next release once *through* the `.appinstaller` (`Add-AppxPackage -AppInstallerFile <url>`) updated in place, preserved project data, and `Package.GetAppInstallerInfo().Uri` then reported the update URI. No uninstall, no data loss. |
| Bundle download tolerates GitHub's `application/octet-stream` + 302 redirect? | ✅ **Confirmed** (was UNCERTAIN). Pointed `<MainBundle Uri>` straight at the GitHub `browser_download_url` and it installed. **No lexbox proxy needed.** |
| HTTP range requests required / a problem? | ✅ Not a problem (was flagged in the controller comment). GitHub's CDN already returns `Accept-Ranges: bytes` + `Content-Length`. |
| Background auto-update cadence | Documented as ~8h via `<AutomaticBackgroundTask>`, only during Windows Automatic Maintenance windows (idle + on power). Force it for testing with `MSchedExe.exe Start`. |

### What was implemented (branch `fwlite-appinstaller`)

1. **`FwLiteReleaseService.GenerateAppInstaller`** — dropped `<OnLaunch>` (no on-launch check → no
   popup, and no race with the in-app updater); kept silent `<AutomaticBackgroundTask>`. Hardened
   `ConvertVersionToAppInstallerVersion` (now `public static`, uses `int.Parse` instead of
   `TrimStart('0')`), locked by a test. The generated `<MainBundle Uri>` points **directly at the
   GitHub release asset** (octet-stream confirmed working).
2. **`AppUpdateService.ApplyUpdate` (FwLiteMaui/Windows)** — two update paths chosen at runtime:
   - On the App Installer track (`Package.Current.GetAppInstallerInfo()?.Uri` is non-null) →
     `AddPackageByAppInstallerFileAsync(uri, …)`. This is mandatory: updating the raw bundle via
     `AddPackageByUriAsync` would **detach** the app from the track.
   - Otherwise (plain `.msixbundle` install, most users today) → the existing
     `AddPackageByUriAsync(bundle)` path, unchanged.
   This gives a **gradual rollout**: bundle users are untouched; testers installed via `.appinstaller`
   self-update through the App Installer API. Shipping the same build later migrates everyone on their
   next update.

### Decisions / rollout

- **Not rolled out to all users yet.** Two install paths coexist deliberately: most users stay on the
  bundle + in-app updater; a few testers use the `.appinstaller`. Once confidence is high, a single
  release migrates the fleet (their in-app updater installs *through* the `.appinstaller`, attaching
  them to the OS track).
- **Coexistence is safe** because the in-app updater now uses the App Installer API when on the track,
  so it never fights or detaches the OS mechanism.

### Watch items / not yet done

- **`packageManagement` capability:** `AddPackageByAppInstallerFileAsync` documents it as required,
  but same-app self-update via `AddPackageByUriAsync` works today without declaring it, so it's
  *probably* fine. If a tester's first in-app update fails with an access/capability error, add the
  `rescap` capability to `Package.appxmanifest`.
- **Download-page link:** new users must install *from the `.appinstaller`* to land on the track — the
  FW Lite download button still needs to point at `…/download-latest?edition=windowsAppInstaller`.
- **`GetLatestRelease(WindowsAppInstaller)` still throws** — harmless today (the controller
  special-cases that edition to `GenerateAppInstaller`), but revisit if other call sites use it.

### ⚠️ Gotcha discovered: MAUI ignores env vars / appsettings

`FwLiteConfig.UpdateCheckCondition` is documented as settable via `FwLite__UpdateCheckCondition`, but
**that only works for FwLiteWeb (ASP.NET Core).** `MauiApp.CreateBuilder()` adds no environment-variable
or appsettings configuration source, so `BindConfiguration("FwLite")` binds against empty config in the
desktop app — the env var is silently ignored. Any behaviour change in the MAUI app must be **code + a
new signed build**; there is no runtime toggle. (An MSIX app launched from the Start menu wouldn't
inherit a shell's env vars anyway.)

---

## Executive summary (answers to the three questions)

### 1. Can FW Lite use `.appinstaller` at all? — **Yes, with caveats.**

The OS-driven `.appinstaller` update mechanism is a supported, documented feature (Windows 10
1709+; the `<UpdateSettings>` knobs FW Lite uses need newer builds — see the version table below).
It requires: a signed bundle trusted by the device (FW Lite already signs via Trusted Signing, so
this is satisfied); a stable, publicly reachable `.appinstaller` URL served as
`application/appinstaller` (the controller already does this); HTTPS/HTTP/SMB hosting; correct
`Content-Type` and `Content-Length` on every file served over HTTP; and byte-range (HTTP/1.1)
support on the server hosting the bundle. **The one thing that no longer works is the
`ms-appinstaller:` one-click-from-browser protocol** — Microsoft disabled it by default in
December 2023 for security reasons. What still works without any special protocol: **downloading
the `.appinstaller` file and double-clicking it**, `Add-AppxPackage -AppInstallerFile`, and the
`PackageManager.AddPackageByAppInstallerFileAsync` API. The OS-scheduled `OnLaunch` /
`AutomaticBackgroundTask` checks also still work once a package is on the App Installer track.

### 2. Can an `.appinstaller` be attached to an app that was already installed from a bare `.msixbundle`? — **Yes, but only by installing/updating *through* the `.appinstaller` once. It cannot be attached passively.**

This is the make-or-break question and the honest answer is: **the App Installer association is
established at install/update time, not retroactively by publishing a file.** First-party docs say
plainly that "**Once the user has installed the application using these steps, the application is
associated with the App Installer file**" and that the install/update "**Create[s] a reference to
the Update and Repair URIs for the package's family**." An app that was installed from a bare
bundle (as FW Lite's current `AppUpdateService` does, via `AddPackageByUriAsync` pointed straight
at the `.msixbundle`) has **no** such association and `Package.GetAppInstallerInfo()` returns
nothing useful for it.

To migrate an existing install onto the track you re-run the install **through** the
`.appinstaller` exactly once — `PackageManager.AddPackageByAppInstallerFileAsync(<appinstaller
uri>, …)`, or `Add-AppxPackage -AppInstallerFile`, or the user double-clicking the file. Because
the package **identity is identical** (`FwLiteDesktop`, same publisher) and the version is equal-or-
higher, this is an in-place MSIX update that **preserves per-user app data** (standard MSIX
same-identity update semantics) and, crucially, **records the App Installer URI on the package** so
that the OS's `OnLaunch` / `AutomaticBackgroundTask` checks take over from then on.

**What I could NOT find in first-party sources:** a single sentence that explicitly says "an app
originally installed from a bare bundle can be retroactively converted to the App Installer track."
The conclusion above is a **strongly-supported inference** from (a) the association-at-install-time
statements, (b) `AddPackageByAppInstallerFileAsync` being the documented install/update entry point,
and (c) standard same-identity MSIX in-place-update semantics — **not a verbatim guarantee**. It
should be validated once on a real machine (install current bundle the old way → install next
release via `Add-AppxPackage -AppInstallerFile` → confirm `Get-AppxPackage FwLiteDesktop |
Select-Object -ExpandProperty ... ` / `Package.GetAppInstallerInfo()` now reports the URI, app data
survived, and a subsequent bump is picked up automatically). **Treat this as the primary de-risking
experiment before committing to the approach.**

### 3. What will it take to finish? — **Modest; the controller's stated blockers are mostly non-blockers.**

- **Range requests are NOT a problem.** GitHub's release-asset CDN already returns
  `Accept-Ranges: bytes` and a correct `Content-Length` (verified live against the current FW Lite
  bundle asset). The controller comment's "we'd need to support range requests… too complicated" is
  moot as long as the bundle URI points at the CDN rather than at a lexbox endpoint that buffers.
- **`Content-Type` is the one real open risk.** Docs say every file must be served with its
  "correct MIME type," and GitHub serves the bundle as `application/octet-stream`, **not**
  `application/msixbundle`. Whether the App Installer download flow actually rejects octet-stream is
  **UNCERTAIN** from first-party docs (the MIME requirement is stated most emphatically in the
  context of the now-disabled `ms-appinstaller` web-page flow). Cheapest safe path: point
  `<MainBundle Uri>` **directly at the GitHub objects/CDN `browser_download_url`** (which it already
  does) and validate on a real machine; if it fails, have lexbox proxy the bundle with
  `Content-Type: application/msixbundle` (range requests can be delegated by 302-redirecting to the
  CDN rather than streaming through lexbox).
- **Version format / `GenerateAppInstaller` bug risk.** Quad-dotted `Major.Minor.Build.Revision` is
  mandatory and the `<MainBundle>` `Version` **must exactly equal the bundle's manifest identity
  version or the install fails**. The `month.TrimStart('0')`/`day.TrimStart('0')` logic is fragile
  and, more importantly, must produce the *same* version string the bundle was actually stamped with
  at build time — see the detailed section for the concrete failure modes.
- **Finish the plumbing:** make `GetLatestRelease(WindowsAppInstaller)` stop throwing (or route it
  differently), keep the self-referencing `Uri` stable and public, and decide the migration UX
  (ship one release that users install once via the `.appinstaller`).
- **Reconcile with the existing in-app updater** so the two don't double-update.

---

## Detailed findings

> **These sections are the original pre-implementation research (2026-07-28), kept for their
> primary-source reasoning.** Where they say "UNCERTAIN", list action items, or describe code/config
> that has since changed (`OnLaunch` removed, `TrimStart('0')` → `int.Parse`), the
> [Status section](#status-verified-and-implemented-2026-07-29) above is authoritative for what
> shipped. Inline ✅ notes flag the items that are now resolved.

### A. Requirements for the `.appinstaller` OS-driven update flow

**Windows version minimums** (from the schema/update-settings docs):

| Feature | Min Windows 10 build |
| --- | --- |
| `.appinstaller` file support at all | 1709 (Fall Creators Update, build 16299) |
| `OnLaunch` | 1709 |
| `HoursBetweenUpdateChecks` / `AutomaticBackgroundTask` | 1803 |
| `ShowPrompt` / `UpdateBlocksActivation` / `ForceUpdateFromAnyVersion` | 1903 |
| `<UpdateUris>` / `<RepairUris>` fallback (2021 schema) | 2004 (build 19041) |

FW Lite's generated file uses the **2021 schema** and `OnLaunch` + `ShowPrompt` +
`UpdateBlocksActivation` + `AutomaticBackgroundTask` + `ForceUpdateFromAnyVersion`, so the effective
floor is **Windows 10 2004 (19041)** — which is also the MAUI target framework floor
(`net11.0-windows10.0.19041.0`), so no new constraint is introduced.

**Signing / trust:** packages must be signed with a certificate trusted by the device. A CA-trusted
cert (Trusted Signing, which FW Lite uses) means Windows already trusts it and nothing needs to be
deployed to devices. (Self-signed would require importing into the *machine* Trusted People store —
not applicable here.)

**Hosting / transport:**
- Downloads/updates support **https, http and smb**.
- Over HTTP, **all files must be served with the correct MIME type** in `Content-Type`, and **all
  responses (GET *and* HEAD) must include a correct `Content-Length`**. Missing `Content-Length`
  is documented to cause `0x80072F76` failures.
- The web-install requirements list **"Web servers need to have support for byte range requests
  (HTTP/1.1)."**
- MIME table (first-party): `.msixbundle` → `application/msixbundle`, `.appinstaller` →
  `application/appinstaller`.

**`ms-appinstaller:` protocol status (important security change):** As of **December 2023**, in App
Installer **1.21.3421.0 and later**, Microsoft **disabled the `ms-appinstaller:?source=` protocol
handler by default on consumer devices** (response to malware abuse — MSRC/CVE-2021-43890 lineage).
The docs' recommended non-enterprise alternatives are (a) publish to the Microsoft Store, or (b)
**"host the `.appinstaller` file on your web server and link to it directly. Users download and
double-click the file; no special protocol is required."** Enterprises can re-enable via Group
Policy `EnableMSAppInstallerProtocol`. FW Lite should assume the protocol is **unavailable** and
rely on file-download-and-open / API-driven installs. (App Installer build 1.24.1981+ also adds
Internet Zone + SmartScreen validation to the protocol path — another reason not to depend on it.)

**Also note the "vanity URL" restriction** (troubleshooting doc): when using the `ms-appinstaller`
protocol the `source` param must literally end in `.appinstaller` and **redirects are not
tolerated**. This is a protocol-path restriction; it's a reason to prefer serving a real
`.appinstaller` URL. It does not obviously apply to the `<MainBundle Uri>` bundle download, but
whether App Installer follows a **302 redirect for the bundle download** is **UNCERTAIN** — worth
testing since FW Lite's controller `download-latest` returns a redirect to GitHub.

### B. `<UpdateSettings>` schema semantics (what FW Lite's file actually asks for)

From the update-settings + schema docs, matched against the generated XML:

- `<OnLaunch HoursBetweenUpdateChecks="8" ShowPrompt="true" UpdateBlocksActivation="false">`
  - Checks for updates when the app launches, at most once per 8-hour window. This check **can show
    UI**.
  - `ShowPrompt="true"` → the user sees update UI.
  - `UpdateBlocksActivation="false"` → the user may launch the app **without** taking the update;
    the update is applied silently at an opportune time. (`UpdateBlocksActivation="true"` would
    require `ShowPrompt="true"` and force the update before launch.)
- `<AutomaticBackgroundTask />` → the OS checks for updates **in the background every 8 hours**
  regardless of whether the app is launched. **Cannot show UI.**
- `<ForceUpdateFromAnyVersion>false</ForceUpdateFromAnyVersion>` → only forward version moves are
  allowed (no downgrades). Fine for FW Lite.

This configuration is coherent and matches Microsoft's documented "silent, non-blocking, checks both
on launch and in the background" pattern.

### C. The "attach to an existing install" mechanics (Question 2, in depth)

Primary-source chain:

1. **Association is created at install time.** "The user must then click on the App Installer
   file… Once the user has installed the application using these steps, the application is
   associated with the App Installer file." And the manual-creation doc: during deployment the file
   will "Create a reference to the Update and Repair URIs for the package's family," after validating
   that the `<MainBundle>` `Name`/`Publisher`/`Version` match the bundle manifest identity (**"If the
   Package/Identity element … do not match, the installation will fail."**).
2. **The association is per-package and inspectable.** `Package.GetAppInstallerInfo()` "retrieves
   the URI to the `.appinstaller` file associated with the current app"; `AppInstallerInfo.UpdateUris`
   exposes 1–10 update URIs. A bare-bundle install has no such info → not on the track.
3. **Code-driven updates must go through the App Installer APIs to stay on the track.** "If you are
   deploying your application using the App Installer file, any code driven updates you perform must
   make use of the App Installer file APIs… Doing so ensures that your regular App Installer file
   updates will continue to work" — i.e. `AddPackageByAppInstallerFileAsync` /
   `RequestAddPackageByAppInstallerFileAsync`, with `Package.CheckUpdateAvailabilityAsync` to poll.
4. **`AddPackageByAppInstallerFileAsync(Uri appInstallerFileUri, AddPackageByAppInstallerOptions
   options, PackageVolume targetVolume)`** — Windows 10 1709+ (10.0.16299.0), requires the
   `packageManagement` restricted capability — "Allows single or multiple app Packages to be
   installed with an `.appinstaller` file." This is the entry point that both installs and updates a
   same-identity package while wiring up the association.

**Therefore:** the migration path for the existing bare-bundle installs is to have each user install
**one** release **through** the `.appinstaller` (double-click, `Add-AppxPackage -AppInstallerFile`,
or an in-app call to `AddPackageByAppInstallerFileAsync`). That single install is an in-place
same-identity update (app data preserved) that moves them onto the OS auto-update track; every
subsequent release is picked up by Windows automatically with no lexbox polling.

**Honesty markers:**
- **UNCERTAIN (needs a machine test):** that a bare-bundle install is *retroactively* convertible
  this way with data preserved. Strongly implied, not verbatim documented. This is the single most
  important thing to prove before building.
- The `packageManagement` capability is "required for cross-publisher scenario, but managing your
  own app should work without having to declare the capability" — FW Lite manages its own same-
  publisher package, so it likely already works (the existing `AppUpdateService` already calls
  `PackageManager` successfully). Confirm the manifest state if you move to the AppInstaller API.

### D. Version format and the `GenerateAppInstaller` risk (Question 3, in depth)

- **Format:** both the `<AppInstaller Version>` and `<MainBundle Version>` must be **quad-dotted
  `Major.Minor.Build.Revision`** (e.g. `2.23.12.43`). Each segment is a 16-bit integer (0–65535).
- **Must match the bundle manifest.** The `<MainBundle>` `Name`/`Publisher`/`Version` **must equal
  the bundle's `Package/Identity`** or the install fails outright. So the version string
  `GenerateAppInstaller` emits must be **byte-for-byte** the version the `.msixbundle` was actually
  stamped with in `.github/workflows/fw-lite.yaml` — not merely "a plausible transform of the git
  tag." ✅ **Resolved:** CI stamps `MakeAppx /bv $(date +%Y.%-m.%-d).1`, which matches what
  `ConvertVersionToAppInstallerVersion` produces (e.g. `2026.7.6.1`); locked by a unit test.
- ✅ **Resolved — `int.Parse` replaced `TrimStart('0')`.** (Original concern, for the record: a `"0"`
  segment would `TrimStart` to the empty string → invalid version; `int.Parse(...).ToString()` avoids
  that. Month/day are never `00` from a real tag, so it was latent, not live.) Still a known
  limitation: the hard-coded `.1` revision means two releases on the **same calendar day** produce the
  **same** version → the second isn't seen as an update. Rare, but real for hotfixes.
- The self-referencing root `Uri` must be a **stable, publicly reachable** URL that returns the
  `.appinstaller` content as `application/appinstaller`. The controller already returns
  `File(..., "application/appinstaller", "FieldWorksLite.appinstaller")`, so **that content-type
  requirement is already met**. Keep the URL (`…/download-latest?edition=windowsAppInstaller`)
  stable across releases, since it is baked into every installed package as the update source.

### E. Original work items — status

1. ✅ **De-risked on a real box.** Bare install → install once via `Add-AppxPackage -AppInstallerFile`
   → in-place update, data preserved, `GetAppInstallerInfo().Uri` populated, background auto-update
   confirmed. (Question 2, answered empirically.)
2. ✅ **Version parity confirmed + `ConvertVersionToAppInstallerVersion` moved to `int.Parse`**, locked
   by a unit test.
3. ✅ **`<MainBundle Uri>` points at the GitHub `browser_download_url` and octet-stream + 302 install
   works** — no proxy needed. The stale controller comment was removed.
4. ⬜ **`GetLatestRelease(WindowsAppInstaller)` still throws** — harmless today (the controller
   special-cases that edition to `GenerateAppInstaller`), left as-is; revisit only if other call sites
   need it.
5. ⬜ **Download-page link** to `…/download-latest?edition=windowsAppInstaller` — deliberately **not**
   done; the appinstaller is kept private to testers for now.
6. ⬜ **Optional hardening** (validate `HoursBetweenUpdateChecks` range, keep the 2021 namespace for
   later `<UpdateUris>`/`<RepairUris>`) — not needed yet.

### F. Interaction with the existing `AppUpdateService.cs` in-app updater

> ✅ **Resolved:** implemented as the two-path updater (see Status section) — the in-app updater uses
> `AddPackageByAppInstallerFileAsync` when on the track and the bundle path otherwise, so the two
> mechanisms don't fight. The analysis below is the original reasoning behind that choice.

The existing updater polls `/api/fwlite-release/should-update` and calls
`PackageManager.AddPackageByUriAsync(<msixbundle url>, …)` with `ForceUpdateFromAnyVersion` and
`DeferRegistrationWhenPackagesAreInUse`. That path installs a bare bundle and **does not** put the
package on the AppInstaller track (and, per the docs, code-driven updates for an AppInstaller-tracked
app *should* use the AppInstaller APIs instead, to keep OS updates working).

**Coexistence:** technically both can run, but it invites **redundant/racing updates** — the lexbox
poller and the OS `OnLaunch`/background task could each try to install the same new version. This is
wasteful and confusing, not a data-loss risk (same identity, in-place). Recommended end state:

- **Either** keep the current in-app updater and **drop the AppInstaller effort** (it already works
  store-lessly), **or**
- **Migrate to the AppInstaller track**: change `AppUpdateService` to install via
  `AddPackageByAppInstallerFileAsync(<appinstaller url>)` (which both updates *and* attaches the
  track), then let the OS handle routine checks and **retire or gate the polling loop** so the two
  mechanisms don't overlap.

Mixing "poll + install bare bundle" with "OS AppInstaller checks" on the same install is the
configuration to avoid.

---

## What could NOT be confirmed from first-party sources — now all machine-verified (2026-07-29)

These were the open items at research time. All three were tested manually and **resolved** — see the
[Status section](#status-verified-and-implemented-2026-07-29) above.

- ~~**Retroactive attachment of a bare-bundle install to the AppInstaller track**~~ (Question 2) —
  ✅ **Confirmed working.** One install through the `.appinstaller` migrated a bare install in place,
  data preserved, `GetAppInstallerInfo().Uri` then populated.
- ~~**Whether the bundle download tolerates `application/octet-stream`**~~ — ✅ **Confirmed working.**
  Installed with `<MainBundle Uri>` pointing straight at the GitHub asset (octet-stream).
- ~~**Whether App Installer follows a 302 redirect for the `<MainBundle Uri>`**~~ — ✅ **Confirmed
  working.** The GitHub `browser_download_url` 302-redirects to the CDN and the install succeeded, so
  no proxy / direct-CDN workaround is required.

---

## Sources

- [App Installer file overview](https://learn.microsoft.com/en-us/windows/msix/app-installer/app-installer-file-overview) — association-at-install-time; hosting protocols (https/http/smb); `ms-appinstaller` disabled Dec 2023.
- [Create an App Installer file manually](https://learn.microsoft.com/en-us/windows/msix/app-installer/how-to-create-appinstaller-file) — quad-dotted version format; Name/Publisher/Version must match `Package/Identity` or install fails; MainBundle/UpdateSettings/UpdateUris/RepairUris examples; 2021-schema requirement for update/repair URIs (Win10 2004+).
- [App Installer file update settings](https://learn.microsoft.com/en-us/windows/msix/app-installer/update-settings) — `OnLaunch`, `HoursBetweenUpdateChecks`, `ShowPrompt`, `UpdateBlocksActivation`, `AutomaticBackgroundTask`, `ForceUpdateFromAnyVersion` semantics and per-feature Windows-version minimums.
- [Installing Windows apps from a web page](https://learn.microsoft.com/en-us/windows/msix/app-installer/installing-windows10-apps-web) — `ms-appinstaller:` disabled by default (Dec 2023, App Installer 1.21.3421.0+); byte-range-request requirement; MIME-type table incl. `application/appinstaller` / `application/msixbundle`; signing/trust; SmartScreen + Internet Zone validation.
- [Update non-Store published apps from your code](https://learn.microsoft.com/en-us/windows/msix/non-store-developer-updates) — code-driven updates for AppInstaller-deployed apps must use the AppInstaller APIs; `AddPackageByAppInstallerFileAsync` / `RequestAddPackageByAppInstallerFileAsync`; `CheckUpdateAvailabilityAsync`; `packageManagement` capability note; `AddPackageByUriAsync`/`AddPackageAsync` for the non-AppInstaller path.
- [PackageManager.AddPackageByAppInstallerFileAsync](https://learn.microsoft.com/en-us/uwp/api/windows.management.deployment.packagemanager.addpackagebyappinstallerfileasync) — signature, Win10 1709 (10.0.16299.0) minimum, `packageManagement` capability.
- [Troubleshoot installation issues with the App Installer file](https://learn.microsoft.com/en-us/windows/msix/app-installer/troubleshoot-appinstaller-issues) — Content-Type "correct MIME type" requirement; Content-Length required on GET and HEAD (0x80072F76); `Add-AppxPackage -AppInstaller` local test; vanity-URL/redirect restriction for `ms-appinstaller`; per-build sideload feature table; trusted-cert store guidance.
- [Package.GetAppInstallerInfo](https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.package.getappinstallerinfo) and [AppInstallerInfo.UpdateUris](https://learn.microsoft.com/en-us/uwp/api/windows.applicationmodel.appinstallerinfo.updateuris) — inspect the per-package AppInstaller association / update URIs (1–10).
- **Live header check (corroborating, not first-party docs):** `HEAD` of the current FW Lite release asset `https://github.com/sillsdev/languageforge-lexbox/releases/download/v2026-07-06-915ca19d/FieldWorksLiteInstaller.msixbundle` → `HTTP 200`, `Accept-Ranges: bytes`, `Content-Length: 172556267`, `Content-Type: application/octet-stream`. Confirms GitHub's CDN supports range requests and correct Content-Length but serves the bundle as octet-stream.
