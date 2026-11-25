namespace FwLiteShared.Services;

/// <summary>
/// Lets the host app wait for the viewer, e.g. to keep a splash screen up until there is something to show.
/// </summary>
public class ViewerLoadState
{
    private readonly TaskCompletionSource _finished = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Completes when the viewer has loaded or failed to load.</summary>
    public Task Finished => _finished.Task;

    public void MarkFinished() => _finished.TrySetResult();
}
