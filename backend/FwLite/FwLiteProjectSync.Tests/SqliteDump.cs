using System.Diagnostics;

namespace FwLiteProjectSync.Tests;

internal static class SqliteDump
{
    public static async Task<string> Dump(string sqlitePath)
    {
        var psi = new ProcessStartInfo("sqlite3")
        {
            ArgumentList = { sqlitePath, ".dump" },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };
        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("sqlite3 CLI not found on PATH");
        // Read both streams concurrently — sequential ReadToEnd deadlocks if pipe buffers fill.
        var stdout = proc.StandardOutput.ReadToEndAsync();
        var stderr = proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();
        if (proc.ExitCode != 0)
            throw new InvalidOperationException($"sqlite3 .dump failed (exit {proc.ExitCode}): {await stderr}");
        return await stdout;
    }
}
