using System.ComponentModel;
using System.Runtime.InteropServices;
using FwLiteShared.KeepAwake;

namespace FwLiteMaui;

/// <summary>
/// Stops Windows from going to sleep on its own while work runs. The user can still sleep it or close the lid.
/// <c>SetThreadExecutionState</c> applies to the calling thread until reset, but hosting may acquire and release
/// from different thread-pool threads, so a dedicated thread holds the state.
/// </summary>
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
