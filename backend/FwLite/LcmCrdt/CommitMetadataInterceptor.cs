using SIL.Harmony.Core;

namespace LcmCrdt;

/// <summary>
/// Async-flow-scoped hook for shaping the <see cref="CommitMetadata"/> of commits written by
/// <see cref="CrdtMiniLcmApi"/> for the duration of an <see cref="Intercept"/> scope — e.g. attributing
/// template-imported system data to the System author. The override lives in an <see cref="AsyncLocal{T}"/>,
/// so it applies only to commits made within the awaited call tree of the scope and can't leak into other
/// operations that share the same DI scope. <see cref="CrdtMiniLcmApi"/> applies it when building metadata.
/// </summary>
public class CommitMetadataInterceptor
{
    private readonly AsyncLocal<Action<CommitMetadata>?> _interceptor = new();

    public IDisposable Intercept(Action<CommitMetadata> interceptor)
    {
        var previous = _interceptor.Value;
        _interceptor.Value = interceptor;
        return new DisposableAction(() => _interceptor.Value = previous);
    }

    public void Apply(CommitMetadata metadata) => _interceptor.Value?.Invoke(metadata);

    private sealed class DisposableAction(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
