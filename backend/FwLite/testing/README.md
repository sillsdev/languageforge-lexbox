# FwLite Windows update test harness

Test the FwLite Windows auto-update flow end-to-end **locally**, without cutting a real GitHub
release or hitting production `lexbox.org`.

It builds two MSIX bundles of FwLite MAUI that share **one** test package identity
(`FwLiteDesktopTest`, distinct from the production `FwLiteDesktop`, so your real install is never
touched) but different versions. You install the lower one, run a fake update server that offers
the higher one, launch the app, and watch it discover → download (through the in-app loopback
progress proxy) → install → **Restart now** onto the new version.

## Contents
- `UpdateTestServer/` — a tiny ASP.NET app that mimics the lexbox update API
  (`GET /api/fwlite-release/should-update`) and serves the `.msixbundle` (with HEAD + Range, which
  the download proxy needs). References `LexCore` so the JSON matches the real API exactly. It is
  **not** in any solution/`.slnf`, so it never touches CI.
- `test-update.ps1` — the interactive, menu-driven harness.

## Prerequisites
- Windows, PowerShell 7 (`pwsh`).
- .NET 10 SDK; the Windows 10 SDK (provides `MakeAppx.exe` / `SignTool.exe`).
- `pnpm` (the harness builds the viewer so the MSIX contains the UI).
- Admin rights **once** to trust the self-signed signing cert (see below).

## Run
```powershell
task fw-lite:test-update      # from repo root, or:
pwsh -NoProfile -File backend/FwLite/testing/test-update.ps1
```

Menu:
1. **Build both MSIX bundles** (v1 low `v0.0.1-test`, v2 high `v0.0.2-test`) — skips if they already exist unless you confirm a rebuild.
2. **Create & trust signing cert** (elevates once).
3. / 4. **Install v1 / v2** (`Add-AppxPackage`).
5. **Uninstall** the test app.
6. / 7. **Start / stop** the fake update server (start prompts which version to serve).
8. **Launch** the installed test app.
9. / 10. **Set / clear** the update env vars that point the app at the local server.

## Typical walkthrough (exercises the Restart-now UI)
1. `1` build both, `2` create+trust cert (accept the admin prompt).
2. `3` install v1. Confirm: `Get-AppxPackage FwLiteDesktopTest` shows `0.0.1.0`.
3. `6` start server, choose `v2`.
4. `9` set env vars, mode **`Never`** (drive the update from the in-app dialog).
5. `8` launch. In the app: **Updates** dialog → *Check* → *Install Update* → watch real download
   progress → **Restart now**. The app relaunches; `Get-AppxPackage FwLiteDesktopTest` now shows
   `0.0.2.0` and the UI shows `v0.0.2-test`.

For the hands-off path, set mode **`Always`** in step 4 instead — the app auto-checks and installs
on launch.

## How it fits together
- The app is redirected with env vars (config section is `FwLite`), set at **User** scope so the
  packaged app inherits them:
  - `FwLite__UpdateUrl=http://localhost:<port>/api/fwlite-release/should-update`
  - `FwLite__UpdateCheckCondition=Never|Always`
- The client does no version comparison of its own, so the server decides. Like the real lexbox API,
  the server only offers the update when the **served version is strictly newer** than the running
  app — it reads the app's current version from the `User-Agent`
  (`Fieldworks-Lite-Client/{version}`) and compares ordinally. So serving **v1** while **v1** is
  installed reports *up-to-date*; serving **v2** prompts. `release.url` points back at the server's
  `/download/<bundle>` endpoint, which the in-app loopback proxy streams for real progress.
- In-place update works because both bundles share identity `FwLiteDesktopTest` with v2 > v1;
  `PackageManager.AddPackageByUriAsync(...ForceUpdateFromAnyVersion=true)` replaces v1 with v2.

## Where things live (and why nothing leaks into git)
- **Signing cert**: the Windows **certificate store** (`CurrentUser\My` for the key;
  `LocalMachine\Root` + `TrustedPeople` for trust). This is global to your machine/user and shared
  across every worktree and clone, so the one-time admin trust step is never repeated. Nothing
  cert-related is committed — no `.gitignore` entry needed. Signing uses the store thumbprint, so
  there's no `.pfx`/password on disk.
- **Harness state** (exported `.cer`, server pid): `%LOCALAPPDATA%\FwLite\update-test-harness\`.
- **Built bundles**: `backend/FwLite/artifacts/harness/` — under the already-gitignored `artifacts/`,
  and deliberately per-worktree so one worktree never reuses another's binaries.

## Cleanup
- Uninstall the test app: menu `5` (or `Get-AppxPackage FwLiteDesktopTest | Remove-AppxPackage`).
- Clear env vars: menu `10`.
- Remove the cert (optional): delete the `CN=FwLiteTestCert` cert from `Cert:\CurrentUser\My`,
  `Cert:\LocalMachine\Root`, and `Cert:\LocalMachine\TrustedPeople`, then delete
  `%LOCALAPPDATA%\FwLite\update-test-harness\`.

## Notes / gotchas
- **Port**: the harness auto-picks a bindable loopback port at startup (preferring 5199, overridable
  with `-Port`). Windows reserves ranges (WinNAT/Hyper-V) whose ports fail to bind with socket error
  10013; the harness detects that and falls back to a free port, and the status header shows the port
  in use. The `FwLite__UpdateUrl` env var is written with that same port, so set it (menu `9`) in the
  same session you start the server.
- **Server crashed on start?** Its stdout/stderr are captured to
  `%LOCALAPPDATA%\FwLite\update-test-harness\server.{out,err}.log`, and the menu prints the tail
  inline if it exits immediately.
- Packaged apps read the **user** environment at activation. If a launched app doesn't pick up new
  env vars, close it fully and relaunch (menu `8`); worst case, sign out/in.
- The signed bundle must be trusted or `Add-AppxPackage` / the in-app updater will reject it — hence
  the cert step. This is expected for any self-signed MSIX.
