using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace FwLiteShared.Pages;

/// <summary>
/// copy of OwningComponentBase, but implements IAsyncDisposable instead of IDisposable
/// </summary>
public abstract class OwningComponentBaseAsync: ComponentBase, IAsyncDisposable
{
    private AsyncServiceScope? _scope;

    [Inject]
    IServiceScopeFactory ScopeFactory { get; set; } = default!;

    /// <summary>
    /// Gets a value determining if the component and associated services have been disposed.
    /// </summary>
    protected bool IsDisposed { get; private set; }

    protected IServiceProvider ScopedServices
    {
        get
        {
            if (ScopeFactory == null)
            {
                throw new InvalidOperationException("Services cannot be accessed before the component is initialized.");
            }

            ObjectDisposedException.ThrowIf(IsDisposed, this);

            _scope ??= ScopeFactory.CreateAsyncScope();
            return _scope.Value.ServiceProvider;
        }
    }
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (!IsDisposed)
        {
            await (_scope?.DisposeAsync() ?? ValueTask.CompletedTask);
            _scope = null;
            IsDisposed = true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
    }
}
