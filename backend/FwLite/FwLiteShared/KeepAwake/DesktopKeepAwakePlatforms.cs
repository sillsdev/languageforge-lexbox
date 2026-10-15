using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Extensions.Logging;

namespace FwLiteShared.KeepAwake;

/// <summary>
/// Stops Windows from going to sleep on its own while work runs. The user can still sleep it or close the lid.
/// <c>SetThreadExecutionState</c> applies to the calling thread until reset, but <see cref="RefCountedKeepAwake"/> may
/// acquire and release from different thread-pool threads, so a dedicated thread holds the state.
/// </summary>
[SupportedOSPlatform("windows")]
internal sealed class WindowsKeepAwakePlatform : IKeepAwakePlatform
{
    [Flags]
    private enum ExecutionState : uint
    {
        SystemRequired = 0x00000001,
        Continuous = 0x80000000,
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern ExecutionState SetThreadExecutionState(ExecutionState flags);

    private ManualResetEventSlim? _release;
    private Thread? _holder;

    public void Acquire(KeepAwakeWork work)
    {
        if (_holder is not null) return;
        var release = new ManualResetEventSlim();
        using var started = new ManualResetEventSlim();
        Exception? failure = null;
        var holder = new Thread(() =>
        {
            if (SetThreadExecutionState(ExecutionState.Continuous | ExecutionState.SystemRequired) == 0)
                failure = new Win32Exception(Marshal.GetLastPInvokeError());
            started.Set();
            if (failure is not null) return;
            release.Wait();
            SetThreadExecutionState(ExecutionState.Continuous);
        }) { IsBackground = true, Name = "Keep awake" };
        holder.Start();
        started.Wait();
        if (failure is not null)
        {
            release.Dispose();
            throw failure;
        }
        _release = release;
        _holder = holder;
    }

    public void Release()
    {
        _release?.Set();
        _holder?.Join(TimeSpan.FromSeconds(5));
        _release?.Dispose();
        _release = null;
        _holder = null;
    }
}

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
                    work.Title, inhibitor.StandardError.ReadToEnd().Trim());
                inhibitor.Dispose();
                return;
            }
            _inhibitor = inhibitor;
        }
        catch (Win32Exception e)
        {
            logger.LogWarning(e, "systemd-inhibit isn't available; this computer may sleep during {WorkTitle}", work.Title);
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
