using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Caching.Memory;

namespace LcmCrdt.FullTextSearch;

public class UpdateEntrySearchTableInterceptor : ISaveChangesInterceptor
{
    private readonly IMemoryCache _cache;

    public UpdateEntrySearchTableInterceptor(IMemoryCache cache, CurrentProjectService currentProjectService)
    {
        _cache = cache;
    }

    private ValueTask<WritingSystems> GetWritingSystems()
    {
        return ValueTask.FromResult(new WritingSystems());
    }

    public InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateSearchTableOnSave(eventData.Context);
        return default;
    }

    public async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = new CancellationToken())
    {
        UpdateSearchTableOnSave(eventData.Context);
        return default;
    }

    private void UpdateSearchTableOnSave(DbContext? eventDataContext)
    {
        if (eventDataContext is null) return;
    }
}
