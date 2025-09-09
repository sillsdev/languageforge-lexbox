using System.ComponentModel;
using SIL.LCModel.Utils;

namespace FwDataMiniLcmBridge.LcmUtils;

public class LcmThreadedProgress : IThreadedProgress
{
    private readonly SingleThreadedSynchronizeInvoke _synchronizeInvoke = new();

#pragma warning disable CS0067
    public event CancelEventHandler? Canceling; // this is part of the interface
#pragma warning restore CS0067

    public void Step(int amount)
    {
    }

    public string? Title { get; set; }
    public string? Message { get; set; }
    public int Position { get; set; }
    public int StepSize { get; set; }
    public int Minimum { get; set; }
    public int Maximum { get; set; }

    public ISynchronizeInvoke SynchronizeInvoke => _synchronizeInvoke;

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

    public bool Canceled => false;

    public bool IsCanceling => false;
}
