using SIL.Harmony.Core;

namespace LcmCrdt;

/// <summary>
/// Scoped hook for shaping the <see cref="CommitMetadata"/> of commits written by
/// <see cref="CrdtMiniLcmApi"/> for the duration of a scope — e.g. attributing template-imported
/// system data to the System author. <see cref="CrdtMiniLcmApi"/> applies it when building metadata.
/// </summary>
public class CommitMetadataInterceptor
{
    private Action<CommitMetadata>? _interceptor;

    public IDisposable Intercept(Action<CommitMetadata> interceptor)
    {
        var previous = _interceptor;
        _interceptor = interceptor;
        return new DisposableAction(() => _interceptor = previous);
    }

    public void Apply(CommitMetadata metadata) => _interceptor?.Invoke(metadata);

    private sealed class DisposableAction(Action onDispose) : IDisposable
    {
        public void Dispose() => onDispose();
    }
}
