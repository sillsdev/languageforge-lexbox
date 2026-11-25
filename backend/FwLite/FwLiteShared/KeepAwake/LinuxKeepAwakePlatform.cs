using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.Versioning;
using LexCore.Utils;
using Microsoft.Extensions.Logging;

namespace FwLiteShared.KeepAwake;

/// <summary>
/// Stops a Linux desktop from going to sleep or idling while work runs, using a logind inhibitor. The inhibitor runs
/// <c>cat</c> reading from our stdin, so if FieldWorks Lite exits, even by crashing, the pipe closes, <c>cat</c> ends
/// and the inhibitor is released. Without systemd-inhibit (no logind) this does nothing beyond a warning.
/// </summary>
[SupportedOSPlatform("linux")]
internal sealed class LinuxKeepAwakePlatform(ILogger<LinuxKeepAwakePlatform> logger) : IKeepAwakePlatform
{
    private Process? _inhibitor;

    public void Acquire(KeepAwakeWork work)
    {
        if (_inhibitor is not null) return;
        var startInfo = new ProcessStartInfo("systemd-inhibit")
        {
            ArgumentList = { "--what=idle:sleep", "--who=FieldWorks Lite", $"--why={work.Title}", "--mode=block", "cat" },
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        try
        {
            var inhibitor = Process.Start(startInfo);
            // logind or polkit can refuse the inhibitor, in which case systemd-inhibit exits straight away
            if (inhibitor is not null && inhibitor.WaitForExit(TimeSpan.FromMilliseconds(300)))
            {
                logger.LogWarning("systemd-inhibit refused to keep this computer awake for {WorkTitle}: {Error}",
                    work.Title.SanitizeForLog(), inhibitor.StandardError.ReadToEnd().Trim().SanitizeForLog());
                inhibitor.Dispose();
                return;
            }
            _inhibitor = inhibitor;
        }
        catch (Win32Exception e)
        {
            logger.LogWarning(e, "systemd-inhibit isn't available; this computer may sleep during {WorkTitle}", work.Title.SanitizeForLog());
        }
    }

    public void Release()
    {
        if (_inhibitor is null) return;
        try
        {
            _inhibitor.StandardInput.Close();
            if (!_inhibitor.WaitForExit(TimeSpan.FromSeconds(5))) _inhibitor.Kill(entireProcessTree: true);
        }
        finally
        {
            _inhibitor.Dispose();
            _inhibitor = null;
        }
    }
}
