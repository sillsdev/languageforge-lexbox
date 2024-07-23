using System.ComponentModel;
using SIL.LCModel.Utils;

namespace FwDataMiniLcmBridge.LcmUtils;

public class LcmThreadedProgress : IThreadedProgress
{
    private SingleThreadedSynchronizeInvoke _synchronizeInvoke = new();

    public event CancelEventHandler? Canceling; // this is part of the interface

    public void Step(int amount)
    {
    }

    public string? Title { get; set; }
    public string? Message { get; set; }
    public int Position { get; set; }
    public int StepSize { get; set; }
    public int Minimum { get; set; }
    public int Maximum { get; set; }

    public ISynchronizeInvoke SynchronizeInvoke
    {
        get { return _synchronizeInvoke; }
    }

    public bool IsIndeterminate { get; set; }
    public bool AllowCancel { get; set; }

    public object RunTask(Func<IThreadedProgress, object[], object> backgroundTask, params object[] parameters)
    {
        return backgroundTask(this, parameters);
    }

    public object RunTask(bool fDisplayUi,
        Func<IThreadedProgress, object[], object> backgroundTask,
        params object[] parameters)
    {
        return backgroundTask(this, parameters);
    }

    public bool Canceled
    {
        get { return false; }
    }

    public bool IsCanceling
    {
        get { return false; }
    }
}
