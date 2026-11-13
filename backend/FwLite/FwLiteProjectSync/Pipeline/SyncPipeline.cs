using LexCore.Sync;
using MiniLcm;

namespace FwLiteProjectSync.Pipeline;

/// <summary>
/// Executes a set of <see cref="SyncStep"/>s in dependency order. Steps run sequentially (sync writes are
/// data-loss-critical, so never in parallel). A step may add further steps while it runs; those join the
/// pool and are scheduled once their ordering constraints are satisfied.
/// </summary>
public static class SyncPipeline
{
    public static async Task<SyncResult> Execute(
        IEnumerable<SyncStep> initialSteps,
        IMiniLcmApi crdtApi,
        IMiniLcmApi fwdataApi,
        ProjectSnapshot snapshot)
    {
        var pending = new List<SyncStep>(initialSteps);
        var completed = new HashSet<string>();

        void AddStep(SyncStep step)
        {
            if (step.Before.Any(completed.Contains))
                throw new InvalidOperationException(
                    $"Sync step '{step.Name}' declares it must run before a step that has already completed.");
            pending.Add(step);
        }

        var context = new SyncStepContext(crdtApi, fwdataApi, snapshot, AddStep);

        while (pending.Count > 0)
        {
            var index = FindReadyStepIndex(pending, completed);
            if (index < 0)
                throw new InvalidOperationException(
                    "Sync pipeline has a cycle or unsatisfiable ordering constraint among steps: " +
                    string.Join(", ", pending.Select(s => s.Name)));

            var step = pending[index];
            pending.RemoveAt(index);
            await step.Execute(context);
            completed.Add(step.Name);
        }

        return new SyncResult(context.CrdtChanges, context.FwdataChanges);
    }

    /// <summary>Index of the first pending step whose predecessors are all complete, or -1 if none is ready.</summary>
    private static int FindReadyStepIndex(List<SyncStep> pending, HashSet<string> completed)
    {
        for (var i = 0; i < pending.Count; i++)
        {
            if (IsReady(pending[i], pending, completed)) return i;
        }

        return -1;
    }

    private static bool IsReady(SyncStep step, List<SyncStep> pending, HashSet<string> completed)
    {
        // Every 'after' dependency that still exists as a step must have completed.
        // A name that matches no step at all is treated as an optional (satisfied) dependency.
        foreach (var afterName in step.After)
        {
            if (completed.Contains(afterName)) continue;
            if (pending.Any(p => p.Name == afterName)) return false;
        }

        // Any still-pending step that must run before this one blocks it.
        foreach (var other in pending)
        {
            if (ReferenceEquals(other, step)) continue;
            if (other.Before.Contains(step.Name)) return false;
        }

        return true;
    }
}
