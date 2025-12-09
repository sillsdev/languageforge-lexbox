using System.Data;
using Gridify;
using LcmCrdt.Changes.Entries;
using LcmCrdt.FullTextSearch;
using LcmCrdt.Utils;
using LinqToDB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MiniLcm.Culture;
using MiniLcm.Exceptions;
using MiniLcm.SyncHelpers;

namespace LcmCrdt.Data;

public class MiniLcmRepositoryFactory(
    IDbContextFactory<LcmCrdtDbContext> dbContextFactory,
    IServiceProvider serviceProvider,
    EntrySearchServiceFactory? entrySearchServiceFactory = null)
{
    public MiniLcmRepository CreateRepo()
    {
        return CreateRepoInternal(dbContextFactory.CreateDbContext());
    }

    public async Task<MiniLcmRepository> CreateRepoAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        return CreateRepoInternal(await dbContextFactory.CreateDbContextAsync(cancellationToken));
    }

    private MiniLcmRepository CreateRepoInternal(LcmCrdtDbContext dbContext)
    {
        if (entrySearchServiceFactory is null) return ActivatorUtilities.CreateInstance<MiniLcmRepository>(serviceProvider, dbContext);
        return ActivatorUtilities.CreateInstance<MiniLcmRepository>(serviceProvider, dbContext, entrySearchServiceFactory.CreateSearchService(dbContext));
    }
}

public class MiniLcmRepository(
    LcmCrdtDbContext dbContext,
    IMiniLcmCultureProvider cultureProvider,
    IOptions<LcmCrdtConfig> config,
    EntrySearchService? entrySearchService = null
) : IAsyncDisposable, IDisposable
{
    public EntrySearchService? SearchService { get; } = entrySearchService;

    private async ValueTask EnsureConnectionOpen()
    {
        if (dbContext.Database.GetDbConnection().State == ConnectionState.Open) return;
        await RelationalDatabaseFacadeExtensions.OpenConnectionAsync(dbContext.Database);
    }

    public ValueTask DisposeAsync()
    {
        return dbContext.DisposeAsync();
    }

    public void Dispose()
    {
        dbContext.Dispose();
    }


    public IQueryable<Entry> Entries => dbContext.Entries;
    public IQueryable<ComplexFormComponent> ComplexFormComponents => dbContext.ComplexFormComponents;
    public IQueryable<ComplexFormType> ComplexFormTypes => dbContext.ComplexFormTypes;
    public IQueryable<Sense> Senses => dbContext.Senses;
    public IQueryable<ExampleSentence> ExampleSentences => dbContext.ExampleSentences;
    public IQueryable<WritingSystem> WritingSystems => dbContext.WritingSystems;
    public IQueryable<WritingSystem> WritingSystemsOrdered => dbContext.WritingSystemsOrdered;
    public IQueryable<SemanticDomain> SemanticDomains => dbContext.SemanticDomains;
    public IQueryable<PartOfSpeech> PartsOfSpeech => dbContext.PartsOfSpeech;
    public IQueryable<Publication> Publications => dbContext.Publications;


    private WritingSystem? _defaultVernacularWs;
    private WritingSystem? _defaultAnalysisWs;
    public async ValueTask<WritingSystem?> GetWritingSystem(WritingSystemId id, WritingSystemType type)
    {
        if (id.Code == "default")
        {
            return type switch
            {
                WritingSystemType.Analysis => _defaultAnalysisWs ??=
                    await AsyncExtensions.FirstOrDefaultAsync(WritingSystemsOrdered, ws => ws.Type == type),
                WritingSystemType.Vernacular => _defaultVernacularWs ??=
                    await AsyncExtensions.FirstOrDefaultAsync(WritingSystemsOrdered, ws => ws.Type == type),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            } ?? throw NotFoundException.ForWs(id, type);
        }

        return await AsyncExtensions.FirstOrDefaultAsync(WritingSystemsOrdered, ws => ws.WsId == id && ws.Type == type);
    }

    public async Task<AddEntryComponentChange> CreateComplexFormComponentChange(
        ComplexFormComponent complexFormComponent,
        BetweenPosition? between = null)
    {
        complexFormComponent.Order = await OrderPicker.PickOrder(
            dbContext.ComplexFormComponents.Where(c => c.ComplexFormEntryId == complexFormComponent.ComplexFormEntryId),
            between);
        return new AddEntryComponentChange(complexFormComponent);
    }

    public async Task<ComplexFormComponent?> FindComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        return await AsyncExtensions.SingleOrDefaultAsync(ComplexFormComponents,
            c =>
            c.ComplexFormEntryId == complexFormComponent.ComplexFormEntryId
            && c.ComponentEntryId == complexFormComponent.ComponentEntryId
            && c.ComponentSenseId == complexFormComponent.ComponentSenseId);
    }

    public async Task<ComplexFormComponent> FindComplexFormComponent(Guid objectId)
    {
        return (await AsyncExtensions.SingleOrDefaultAsync(ComplexFormComponents, c => c.Id == objectId)) ??
               throw NotFoundException.ForType<ComplexFormComponent>(objectId);
    }

    public async Task<int> CountEntries(string? query = null, FilterQueryOptions? options = null)
    {
        options ??= FilterQueryOptions.Default;
        var (queryable, _) = await FilterEntries(Entries, query, options);
        return await AsyncExtensions.CountAsync(queryable);
    }

    public async IAsyncEnumerable<Entry> GetEntries(
        string? query = null,
        QueryOptions? options = null)
    {
        options = await EnsureWritingSystemIsPopulated(options ??= QueryOptions.Default);

        var queryable = Entries;
        (queryable, var sortingHandled) = await FilterEntries(queryable, query, options, options.Order);
        if (!sortingHandled)
            queryable = await ApplySorting(queryable, options, query);

        queryable = queryable
            .LoadWith(e => e.Senses)
            .ThenLoad(s => s.ExampleSentences)
            .LoadWith(e => e.Senses).ThenLoad(s => s.PartOfSpeech)
            .LoadWith(e => e.ComplexForms)
            .LoadWith(e => e.Components)
            .AsQueryable();

        queryable = options.ApplyPaging(queryable);
        var complexFormComparer = cultureProvider.GetCompareInfo(await GetWritingSystem(default, WritingSystemType.Vernacular))
            .AsComplexFormComparer();
        var entries = AsyncExtensions.AsAsyncEnumerable(queryable);
        await EnsureConnectionOpen();//sometimes there can be a race condition where the collations arent setup
        await foreach (var entry in EfExtensions.SafeIterate(entries))
        {
            entry.Finalize(complexFormComparer);
            yield return entry;
        }
    }

    private async Task<QueryOptions> EnsureWritingSystemIsPopulated(QueryOptions queryOptions)
    {
        if (queryOptions.Order.WritingSystem != default) return queryOptions;

        var writingSystem = await GetWritingSystem(default, WritingSystemType.Vernacular)
            ?? throw NotFoundException.ForWs(default, WritingSystemType.Vernacular);
        var order = queryOptions.Order with { WritingSystem = writingSystem.WsId };
        return queryOptions with { Order = order };
    }

    private async Task<(IQueryable<Entry> queryable, bool sortingHandled)> FilterEntries(IQueryable<Entry> queryable,
        string? query,
        FilterQueryOptions options,
        SortOptions? sortOptions = null)
    {
        if (options.Exemplar is not null)
        {
            var ws = (await GetWritingSystem(options.Exemplar.WritingSystem, WritingSystemType.Vernacular))?.WsId
                ?? throw NotFoundException.ForWs(options.Exemplar.WritingSystem, WritingSystemType.Vernacular);
            queryable = queryable.WhereExemplar(ws, options.Exemplar.Value);
        }

        if (options.Filter?.GridifyFilter != null)
        {
            // Do this BEFORE doing the FTS, which returns an expression that confuses the gridify query
            queryable = queryable.ApplyFiltering(options.Filter.GridifyFilter, config.Value.Mapper);
        }

        bool sortingHandled = false;
        if (!string.IsNullOrEmpty(query))
        {
            if (SearchService is not null && SearchService.ValidSearchTerm(query))
            {
                if (sortOptions is not null && sortOptions.Field == SortField.SearchRelevance)
                {
                    //ranking must be done at the same time as part of the full-text search, so we can't use normal sorting
                    sortingHandled = true;
                    queryable = SearchService.FilterAndRank(queryable, query, sortOptions.WritingSystem);
                }
                else
                {
                    queryable = SearchService.Filter(queryable, query);
                }
            }
            else
            {
                queryable = queryable.Where(Filtering.SearchFilter(query));
            }
        }

        return (queryable, sortingHandled);
    }

    private ValueTask<IQueryable<Entry>> ApplySorting(IQueryable<Entry> queryable, QueryOptions options, string? query = null)
    {
        if (options.Order.WritingSystem == default)
            throw new ArgumentException("Sorting writing system must be specified", nameof(options));

        var wsId = options.Order.WritingSystem;
        IQueryable<Entry> result = options.Order.Field switch
        {
            SortField.SearchRelevance => queryable.ApplyRoughBestMatchOrder(options.Order, query),
            SortField.Headword =>
                options.ApplyOrder(queryable, e => e.Headword(wsId).CollateUnicode(wsId)).ThenBy(e => e.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(options), "sort field unknown " + options.Order.Field)
        };
        return new ValueTask<IQueryable<Entry>>(result);
    }

    public async Task<Entry?> GetEntry(Guid id)
    {
        var entry = await AsyncExtensions.SingleOrDefaultAsync(Entries
                .LoadWith(e => e.Senses)
                .ThenLoad(s => s.ExampleSentences)
                .LoadWith(e => e.Senses).ThenLoad(s => s.PartOfSpeech)
                .LoadWith(e => e.ComplexForms)
                .LoadWith(e => e.Components)
                .AsQueryable(), e => e.Id == id);
        if (entry is not null)
        {
            var sortWs = await GetWritingSystem(WritingSystemId.Default, WritingSystemType.Vernacular);
            var complexFormComparer = cultureProvider.GetCompareInfo(sortWs)
                .AsComplexFormComparer();
            entry.Finalize(complexFormComparer);
        }

        return entry;
    }

    public async Task<Sense?> GetSense(Guid entryId, Guid senseId)
    {
        var sense = await AsyncExtensions.SingleOrDefaultAsync(Senses.LoadWith(s => s.PartOfSpeech)
                .AsQueryable(), e => e.Id == senseId);
        sense?.Finalize();
        return sense;
    }

    public async Task<ExampleSentence?> GetExampleSentence(Guid entryId, Guid senseId, Guid id)
    {
        var exampleSentence = await AsyncExtensions.SingleOrDefaultAsync(ExampleSentences
                .AsQueryable(), e => e.Id == id);
        exampleSentence?.Finalize();
        return exampleSentence;
    }

    public async Task<(int RowIndex, Entry Entry)> GetEntryRowIndex(Guid entryId, string? query = null, QueryOptions? options = null)
    {
        // This is a fallback implementation that's not optimal for large datasets,
        // but it works correctly. Ideally, we'd use ROW_NUMBER() window function with linq2db
        // for better performance on large entry lists. For now, we enumerate through sorted entries
        // and count until we find the target entry.
        
        var rowIndex = 0;
        await foreach (var entry in GetEntries(query, options))
        {
            if (entry.Id == entryId)
            {
                var fullEntry = await GetEntry(entryId);
                if (fullEntry is null)
                    throw NotFoundException.ForType<Entry>(entryId);
                return (rowIndex, fullEntry);
            }
            rowIndex++;
        }

        throw NotFoundException.ForType<Entry>(entryId);
    }

    public async Task<Publication?> GetPublication(Guid publicationId)
    {
        var publication = await AsyncExtensions.SingleOrDefaultAsync(Publications
                .AsQueryable(), p => p.Id == publicationId);
        return publication;
    }
}
