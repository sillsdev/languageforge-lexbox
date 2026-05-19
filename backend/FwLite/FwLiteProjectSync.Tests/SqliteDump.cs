using System.Diagnostics;

namespace FwLiteProjectSync.Tests;

/// <summary>
/// Test-only helper for issue #1920: dump a CRDT sqlite project as SQL text via the
/// <c>sqlite3</c> CLI. The dump is fed through Verify's inline-Guid scrubber to produce
/// the shipped template artifact; applying that artifact is handled by the production
/// <see cref="LcmCrdt.Project.ProjectTemplate.Apply"/>.
/// </summary>
internal static class SqliteDump
{
    public static string Dump(string sqlitePath)
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
        var output = proc.StandardOutput.ReadToEnd();
        var error = proc.StandardError.ReadToEnd();
        proc.WaitForExit();
        if (proc.ExitCode != 0)
            throw new InvalidOperationException(
                $"sqlite3 .dump failed (exit {proc.ExitCode}): {error}");
        return output;
    }
}
