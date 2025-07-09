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
    Microsoft.EntityFrameworkCore.IDbContextFactory<LcmCrdtDbContext> dbContextFactory,
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
                    await AsyncExtensions.FirstOrDefaultAsync(dbContext.WritingSystems, ws => ws.Type == type),
                WritingSystemType.Vernacular => _defaultVernacularWs ??=
                    await AsyncExtensions.FirstOrDefaultAsync(dbContext.WritingSystems, ws => ws.Type == type),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            } ?? throw new NullReferenceException($"Unable to find a default writing system of type {type}");
        }

        return await AsyncExtensions.FirstOrDefaultAsync(dbContext.WritingSystems, ws => ws.WsId == id && ws.Type == type);
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
               throw NotFoundException.ForType<ComplexFormComponent>();
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
        options ??= QueryOptions.Default;
        var queryable = Entries;
        (queryable, var sortingHandled) = await FilterEntries(queryable, query, options);
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
            entry.ApplySortOrder(complexFormComparer);
            yield return entry;
        }
    }

    private async Task<(IQueryable<Entry> queryable, bool sortingHandled)> FilterEntries(IQueryable<Entry> queryable,
        string? query,
        FilterQueryOptions options)
    {
        bool sortingHandled = false;
        if (!string.IsNullOrEmpty(query))
        {
            if (SearchService is not null && SearchService.ValidSearchTerm(query))
            {
                var queryOptions = options as QueryOptions;
                //ranking must be done at the same time as part of the full-text search, so we can't use normal sorting
                sortingHandled = queryOptions?.Order.Field == SortField.SearchRelevance;
                queryable = SearchService.FilterAndRank(queryable,
                    query,
                    sortingHandled,
                    queryOptions?.Order.Ascending == true);
            }
            else
            {
                queryable = queryable.Where(Filtering.SearchFilter(query));
            }
        }

        if (options.Exemplar is not null)
        {
            var ws = (await GetWritingSystem(options.Exemplar.WritingSystem, WritingSystemType.Vernacular))?.WsId;
            if (ws is null)
                throw new NullReferenceException($"writing system {options.Exemplar.WritingSystem} not found");
            queryable = queryable.WhereExemplar(ws.Value, options.Exemplar.Value);
        }

        if (options.Filter?.GridifyFilter != null)
        {
            queryable = queryable.ApplyFiltering(options.Filter.GridifyFilter, config.Value.Mapper);
        }
        return (queryable, sortingHandled);
    }

    private async ValueTask<IQueryable<Entry>> ApplySorting(IQueryable<Entry> queryable, QueryOptions options, string? query = null)
    {
        var sortWs = await GetWritingSystem(options.Order.WritingSystem, WritingSystemType.Vernacular);
        if (sortWs is null)
            throw new NullReferenceException($"sort writing system {options.Order.WritingSystem} not found");

        switch (options.Order.Field)
        {
            case SortField.SearchRelevance:
                return queryable.ApplyRoughBestMatchOrder(options.Order with { WritingSystem = sortWs.WsId }, query);
            case SortField.Headword:
                var ordered = options.ApplyOrder(queryable, e => e.Headword(sortWs.WsId).CollateUnicode(sortWs));
                return ordered.ThenBy(e => e.Id);
            default:
                throw new ArgumentOutOfRangeException(nameof(options), "sort field unknown " + options.Order.Field);
        }
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
            entry.ApplySortOrder(complexFormComparer);
        }

        return entry;
    }

    public async Task<Sense?> GetSense(Guid entryId, Guid senseId)
    {
        var sense = await AsyncExtensions.SingleOrDefaultAsync(Senses.LoadWith(s => s.PartOfSpeech)
                .AsQueryable(), e => e.Id == senseId);
        sense?.ApplySortOrder();
        return sense;
    }

    public async Task<ExampleSentence?> GetExampleSentence(Guid entryId, Guid senseId, Guid id)
    {
        var exampleSentence = await AsyncExtensions.SingleOrDefaultAsync(ExampleSentences
                .AsQueryable(), e => e.Id == id);
        return exampleSentence;
    }
}
