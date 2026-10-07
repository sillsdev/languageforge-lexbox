# FieldWorks Lite troubleshooting: logs and settings

Applies to the desktop and mobile app (`backend/FwLite/FwLiteMaui`). FW Lite Web is configured through its own
`appsettings.json` and is not covered here.

## Where the files are

Everything lives in the app data directory. The quickest way to find it: open the app, open **Troubleshoot**
(home page menu, or a project's sidebar) and use **Open Data Directory** / **Open Log file** / **Share Log file**.

| Platform | Data directory |
|----------|----------------|
| Windows (Store / MSIX) | `%LOCALAPPDATA%\Packages\<package id>\LocalState` |
| Windows (portable) | the folder the exe runs from |
| macOS | the app's sandboxed `Library/Application Support` folder |
| Android / iOS | app-private storage; use **Share Log file** instead of browsing |

Files of interest:

- `app.log` (and `app1.log`, the previous roll) - the application log
- `fw-lite-settings.json` - optional settings file, see below

## Turning on verbose logging

Ask the user to open **Troubleshoot** and switch on **Verbose logging**. It takes effect immediately, no restart.
It raises everything to `Debug`, including the sign-in (MSAL) logs that are hidden by default. Once the log has
been shared, switch it off again: verbose logs are large and the log rolls at 50 MB.

Behind the switch the app writes `fw-lite-settings.json` in the data directory:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "FwLiteShared.Auth.LoggerAdapter": "Debug"
    }
  }
}
```

## The settings file

Support can send the user a snippet to paste into **Troubleshoot > Advanced: settings file**; **Save and apply**
replaces the file and applies it without a restart (on every platform). The file can also be edited by hand
(comments and trailing commas are allowed); on Windows and macOS hand edits are picked up while the app runs, on
Android and iOS restart the app.

Settings are applied in this order, later wins:

1. built-in defaults (`Default` = `Information`, MSAL and EF Core database logs = `Warning`)
2. `fw-lite-settings.json`
3. environment variables, e.g. `Logging__LogLevel__Default=Debug` or `FwLiteMaui__BaseDataDir=...`

Any configuration section can be set in the file, exactly as with environment variables. At startup `app.log`
lists which top-level sections the file contains. The ones support is most likely to need:

| Setting | Purpose |
|---------|---------|
| `Logging:LogLevel:<category>` | any log category, e.g. `Default`, `FwLiteShared.Auth.LoggerAdapter` (MSAL), `LcmCrdt`, `FwLiteShared`, `Microsoft.EntityFrameworkCore.Database` |
| `FwLiteMaui:MaxLogFileSize` | bytes per log file before it rolls (default 50 MB) |
| `FwLiteMaui:MaxLogFileCount` | how many rolled log files to keep (default 2) |
| `FwDataBridge:ProjectsFolder` | where FieldWorks projects are loaded from (Windows) |

Treat a pasted snippet like any other instruction from a stranger: sections such as `Auth` change which server
the app signs in to.

Example: only MSAL at `Debug`, everything else unchanged, and keep 4 log files:

```json
{
  "Logging": { "LogLevel": { "FwLiteShared.Auth.LoggerAdapter": "Debug" } },
  "FwLiteMaui": { "MaxLogFileCount": 4 }
}
```

The data directory itself should be moved with the `FwLiteMaui__BaseDataDir` environment variable rather than
the file, because the file is looked up in the default (or env-var) directory before it is read.
