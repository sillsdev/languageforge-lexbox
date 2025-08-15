using SIL.Progress;

namespace FwHeadless.Services;

public class CombiningProgress(params IEnumerable<IProgress?> progresses) : IProgress
{

    public bool ShowVerbose
    {
        set { foreach (var p in progresses) { if (p is not null) p.ShowVerbose = value; } }
    }

    public bool CancelRequested
    {
        get => progresses.Any(p => p?.CancelRequested ?? false);
        set { foreach (var p in progresses) { if (p is not null) p.CancelRequested = value; } }
    }

    public bool ErrorEncountered
    {
        get => progresses.Any(p => p?.ErrorEncountered ?? false);
        set { foreach (var p in progresses) { if (p is not null) p.ErrorEncountered = value; } }
    }

    public IProgressIndicator ProgressIndicator
    {
        get => progresses.FirstOrDefault(p => p?.ProgressIndicator is not null)?.ProgressIndicator ?? null!;
        set { foreach (var p in progresses) { if (p is not null) p.ProgressIndicator = value; } }
    }

    public SynchronizationContext SyncContext
    {
        get => progresses.FirstOrDefault(p => p?.SyncContext is not null)?.SyncContext ?? null!;
        set { foreach (var p in progresses) { if (p is not null) p.SyncContext = value; } }
    }

    public void WriteError(string message, params object[] args)
    {
        foreach (var p in progresses) { p?.WriteError(message, args); }
    }

    public void WriteException(Exception error)
    {
        foreach (var p in progresses) { p?.WriteException(error); }
    }

    public void WriteMessage(string message, params object[] args)
    {
        foreach (var p in progresses) { p?.WriteMessage(message, args); }
    }

    public void WriteMessageWithColor(string colorName, string message, params object[] args)
    {
        foreach (var p in progresses) { p?.WriteMessageWithColor(colorName, message, args); }
    }

    public void WriteStatus(string message, params object[] args)
    {
        foreach (var p in progresses) { p?.WriteStatus(message, args); }
    }

    public void WriteVerbose(string message, params object[] args)
    {
        foreach (var p in progresses) { p?.WriteVerbose(message, args); }
    }

    public void WriteWarning(string message, params object[] args)
    {
        foreach (var p in progresses) { p?.WriteWarning(message, args); }
    }
}
