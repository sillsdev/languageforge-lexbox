using System.Collections.Frozen;
using System.Globalization;
using System.Reflection;
using System.Text;
using FluentValidation;
using FwDataMiniLcmBridge.Api.UpdateProxy;
using FwDataMiniLcmBridge.LcmUtils;
using Microsoft.Extensions.Logging;
using MiniLcm;
using MiniLcm.Exceptions;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using MiniLcm.Validators;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace FwDataMiniLcmBridge.Api;

public class FwDataMiniLcmApi(Lazy<LcmCache> cacheLazy, bool onCloseSave, ILogger<FwDataMiniLcmApi> logger, FwDataProject project, MiniLcmValidators validators) : IMiniLcmApi, IDisposable
{
    internal LcmCache Cache => cacheLazy.Value;
    public FwDataProject Project { get; } = project;
    public Guid ProjectId => Cache.LangProject.Guid;

    private IWritingSystemContainer WritingSystemContainer => Cache.ServiceLocator.WritingSystems;
    internal ILexEntryRepository EntriesRepository => Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
    internal IRepository<ILexSense> SenseRepository => Cache.ServiceLocator.GetInstance<IRepository<ILexSense>>();
    private IRepository<ILexExampleSentence> ExampleSentenceRepository => Cache.ServiceLocator.GetInstance<IRepository<ILexExampleSentence>>();
    private ILexEntryFactory LexEntryFactory => Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
    private ILexSenseFactory LexSenseFactory => Cache.ServiceLocator.GetInstance<ILexSenseFactory>();
    private ILexExampleSentenceFactory LexExampleSentenceFactory => Cache.ServiceLocator.GetInstance<ILexExampleSentenceFactory>();
    private IMoMorphTypeRepository MorphTypeRepository => Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
    private IPartOfSpeechRepository PartOfSpeechRepository => Cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>();
    private ILexEntryTypeRepository LexEntryTypeRepository => Cache.ServiceLocator.GetInstance<ILexEntryTypeRepository>();
    private ICmSemanticDomainRepository SemanticDomainRepository => Cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>();
    private ICmTranslationFactory CmTranslationFactory => Cache.ServiceLocator.GetInstance<ICmTranslationFactory>();
    private ICmPossibilityRepository CmPossibilityRepository => Cache.ServiceLocator.GetInstance<ICmPossibilityRepository>();
    private ICmPossibilityList ComplexFormTypes => Cache.LangProject.LexDbOA.ComplexEntryTypesOA;
    private IEnumerable<ILexEntryType> ComplexFormTypesFlattened => ComplexFormTypes.PossibilitiesOS.Cast<ILexEntryType>().Flatten();

    private ICmPossibilityList VariantTypes => Cache.LangProject.LexDbOA.VariantEntryTypesOA;

    public void Dispose()
    {
        if (onCloseSave && cacheLazy.IsValueCreated)
        {
            Save();
        }
    }

    public void Save()
    {
        if (Cache.IsDisposed) return;
        logger.LogInformation("Saving FW data file {Name}", Cache.ProjectId.Name);
        Cache.ActionHandlerAccessor.Commit();
    }

    public int EntryCount => EntriesRepository.Count;

    internal WritingSystemId GetWritingSystemId(int ws)
    {
        return Cache.ServiceLocator.WritingSystemManager.Get(ws).Id;
    }

    internal int GetWritingSystemHandle(WritingSystemId ws, WritingSystemType? type = null)
    {
        var lcmWs = GetLcmWritingSystem(ws, type) ?? throw new NullReferenceException($"Unable to find writing system with id {ws}");
        return lcmWs.Handle;
    }


    internal CoreWritingSystemDefinition? GetLcmWritingSystem(WritingSystemId ws, WritingSystemType? type = null)
    {
        if (ws == "default")
        {
            return type switch
            {
                WritingSystemType.Analysis => WritingSystemContainer.DefaultAnalysisWritingSystem,
                WritingSystemType.Vernacular => WritingSystemContainer.DefaultVernacularWritingSystem,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        var lcmWs = Cache.ServiceLocator.WritingSystemManager.Get(ws.Code);
        if (lcmWs is not null && type is not null)
        {
            var validWs = type switch
            {
                WritingSystemType.Analysis => WritingSystemContainer.AnalysisWritingSystems,
                WritingSystemType.Vernacular => WritingSystemContainer.VernacularWritingSystems,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
            if (!validWs.Contains(lcmWs))
            {
                throw new InvalidOperationException($"Writing system {ws} is not of the requested type: {type}.");
            }
        }
        return lcmWs;
    }

    public Task<WritingSystems> GetWritingSystems()
    {
        var currentVernacularWs = WritingSystemContainer
            .CurrentVernacularWritingSystems
            .Select(ws => ws.Id).ToHashSet();
        var currentAnalysisWs = WritingSystemContainer
            .CurrentAnalysisWritingSystems
            .Select(ws => ws.Id).ToHashSet();
        var writingSystems = new WritingSystems
        {
            Vernacular = WritingSystemContainer.CurrentVernacularWritingSystems.Select((definition, index) =>
                FromLcmWritingSystem(definition, index, WritingSystemType.Vernacular)).ToArray(),
            Analysis = Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Select((definition, index) =>
                FromLcmWritingSystem(definition, index, WritingSystemType.Analysis)).ToArray()
        };
        CompleteExemplars(writingSystems);
        return Task.FromResult(writingSystems);
    }

    private WritingSystem FromLcmWritingSystem(CoreWritingSystemDefinition ws, int index, WritingSystemType type)
    {
        return new WritingSystem
        {
            Id = Guid.Empty,
            Order = index,
            Type = type,
            //todo determine current and create a property for that.
            WsId = ws.Id,
            Name = ws.LanguageTag,
            Abbreviation = ws.Abbreviation,
            Font = ws.DefaultFontName,
            Exemplars = ws.CharacterSets.FirstOrDefault(s => s.Type == "index")?.Characters.ToArray() ?? []
        };
    }

    public Task<WritingSystem> GetWritingSystem(WritingSystemId id, WritingSystemType type)
    {
        throw new NotImplementedException();
    }

    internal void CompleteExemplars(WritingSystems writingSystems)
    {
        var wsExemplars = writingSystems.Vernacular.Concat(writingSystems.Analysis)
            .DistinctBy(ws => ws.WsId)
            .ToDictionary(ws => ws, ws => ws.Exemplars.Select(s => s[0]).ToHashSet());
        var wsExemplarsByHandle = wsExemplars.ToFrozenDictionary(kv => GetWritingSystemHandle(kv.Key.WsId), kv => kv.Value);

        foreach (var entry in EntriesRepository.AllInstances())
        {
            LcmHelpers.ContributeExemplars(entry.CitationForm, wsExemplarsByHandle);
            LcmHelpers.ContributeExemplars(entry.LexemeFormOA.Form, wsExemplarsByHandle);
        }

        foreach (var ws in wsExemplars.Keys)
        {
            ws.Exemplars = [.. wsExemplars[ws].Order().Select(s => s.ToString())];
        }
    }

    public Task<WritingSystem> CreateWritingSystem(WritingSystemType type, WritingSystem writingSystem)
    {
        var exitingWs = type == WritingSystemType.Analysis ? Cache.ServiceLocator.WritingSystems.AnalysisWritingSystems : Cache.ServiceLocator.WritingSystems.VernacularWritingSystems;
        if (exitingWs.Any(ws => ws.Id == writingSystem.WsId))
        {
            throw new DuplicateObjectException($"Writing system {writingSystem.WsId.Code} already exists");
        }
        CoreWritingSystemDefinition? ws = null;
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Create Writing System",
            "Remove writing system",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                Cache.ServiceLocator.WritingSystemManager.GetOrSet(writingSystem.WsId.Code, out ws);
                ws.Abbreviation = writingSystem.Abbreviation;
                switch (type)
                {
                    case WritingSystemType.Analysis:
                        Cache.ServiceLocator.WritingSystems.AddToCurrentAnalysisWritingSystems(ws);
                        break;
                    case WritingSystemType.Vernacular:
                        Cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(ws);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), type, null);
                }
            });
        if (ws is null) throw new InvalidOperationException("Writing system not found");
        var index = type switch
        {
            WritingSystemType.Analysis => WritingSystemContainer.CurrentAnalysisWritingSystems.Count,
            WritingSystemType.Vernacular => WritingSystemContainer.CurrentVernacularWritingSystems.Count,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        } - 1;
        return Task.FromResult(FromLcmWritingSystem(ws, index, type));
    }

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystemId id, WritingSystemType type, UpdateObjectInput<WritingSystem> update)
    {
        if (!Cache.ServiceLocator.WritingSystemManager.TryGet(id.Code, out var lcmWritingSystem))
        {
            throw new InvalidOperationException($"Writing system {id.Code} not found");
        }
        await Cache.DoUsingNewOrCurrentUOW("Update WritingSystem",
            "Revert WritingSystem",
            async () =>
            {
                var updateProxy = new UpdateWritingSystemProxy(lcmWritingSystem, this)
                {
                    Id = Guid.Empty,
                    Type = type,
                };
                update.Apply(updateProxy);
                updateProxy.CommitUpdate(Cache);
            });
        return await GetWritingSystem(id, type);
    }

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after)
    {
        await Cache.DoUsingNewOrCurrentUOW("Update WritingSystem",
            "Revert WritingSystem",
            async () =>
            {
                await WritingSystemSync.Sync(after, before, this);
            });
        return await GetWritingSystem(after.WsId, after.Type) ?? throw new NullReferenceException($"unable to find {after.Type} writing system with id {after.WsId}");
    }

    public IAsyncEnumerable<PartOfSpeech> GetPartsOfSpeech()
    {
        return PartOfSpeechRepository
            .AllInstances()
            .OrderBy(p => p.Name.BestAnalysisAlternative.Text)
            .ToAsyncEnumerable()
            .Select(FromLcmPartOfSpeech);
    }

    public Task<PartOfSpeech?> GetPartOfSpeech(Guid id)
    {
        return Task.FromResult(
            PartOfSpeechRepository
            .TryGetObject(id, out var partOfSpeech)
            ? FromLcmPartOfSpeech(partOfSpeech) : null);
    }

    public Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        IPartOfSpeech? lcmPartOfSpeech = null;
        if (partOfSpeech.Id == default) partOfSpeech.Id = Guid.NewGuid();
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Create Part of Speech",
            "Remove part of speech",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                lcmPartOfSpeech = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>()
                    .Create(partOfSpeech.Id, Cache.LangProject.PartsOfSpeechOA);
                UpdateLcmMultiString(lcmPartOfSpeech.Name, partOfSpeech.Name);
            });
        return Task.FromResult(FromLcmPartOfSpeech(lcmPartOfSpeech ?? throw new InvalidOperationException("Part of speech was not created")));
    }

    public Task<PartOfSpeech> UpdatePartOfSpeech(Guid id, UpdateObjectInput<PartOfSpeech> update)
    {
        var lcmPartOfSpeech = PartOfSpeechRepository.GetObject(id);
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Update Part of Speech",
            "Revert Part of Speech",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                var updateProxy = new UpdatePartOfSpeechProxy(lcmPartOfSpeech, this);
                update.Apply(updateProxy);
            });
        return Task.FromResult(FromLcmPartOfSpeech(lcmPartOfSpeech));
    }

    public async Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after)
    {
        await PartOfSpeechSync.Sync(before, after, this);
        return await GetPartOfSpeech(after.Id) ?? throw new NullReferenceException($"unable to find part of speech with id {after.Id}");
    }

    public Task DeletePartOfSpeech(Guid id)
    {
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Delete Part of Speech",
            "Revert delete",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                PartOfSpeechRepository.GetObject(id).Delete();
            });
        return Task.CompletedTask;
    }

    internal SemanticDomain FromLcmSemanticDomain(ICmSemanticDomain semanticDomain)
    {
        return new SemanticDomain
        {
            Id = semanticDomain.Guid,
            Name = FromLcmMultiString(semanticDomain.Name),
            Code = semanticDomain.Abbreviation.UiString ?? "",
            Predefined = true, // TODO: Look up in a GUID list of predefined data
        };
    }

    public IAsyncEnumerable<SemanticDomain> GetSemanticDomains()
    {
        return
            SemanticDomainRepository
            .AllInstances()
            .OrderBy(p => p.Abbreviation.UiString)
            .ToAsyncEnumerable()
            .Select(FromLcmSemanticDomain);
    }

    public Task<SemanticDomain?> GetSemanticDomain(Guid id)
    {
        var semDom = GetLcmSemanticDomain(id);
        return Task.FromResult(semDom is null ? null : FromLcmSemanticDomain(semDom));
    }

    public async Task<SemanticDomain> CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        if (semanticDomain.Id == Guid.Empty) semanticDomain.Id = Guid.NewGuid();
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Create Semantic Domain",
            "Remove semantic domain",
            Cache.ActionHandlerAccessor,
            () =>
            {
                var lcmSemanticDomain = Cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>()
                    .Create(semanticDomain.Id, Cache.LangProject.SemanticDomainListOA);
                lcmSemanticDomain.OcmCodes = semanticDomain.Code;
                UpdateLcmMultiString(lcmSemanticDomain.Name, semanticDomain.Name);
                // TODO: Find out if semantic domains are guaranteed to have an "en" writing system, or if we should use lcmCache.DefautlAnalWs instead
                UpdateLcmMultiString(lcmSemanticDomain.Abbreviation, new MultiString(){{"en", semanticDomain.Code}});
            });
        return await GetSemanticDomain(semanticDomain.Id) ?? throw new InvalidOperationException("Semantic domain was not created");
    }

    public Task<SemanticDomain> UpdateSemanticDomain(Guid id, UpdateObjectInput<SemanticDomain> update)
    {
        var lcmSemanticDomain = SemanticDomainRepository.GetObject(id);
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Update Semantic Domain",
            "Revert Semantic Domain",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                var updateProxy = new UpdateSemanticDomainProxy(lcmSemanticDomain, this);
                update.Apply(updateProxy);
            });
        return Task.FromResult(FromLcmSemanticDomain(lcmSemanticDomain));
    }

    public async Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after)
    {
        await SemanticDomainSync.Sync(before, after, this);
        return await GetSemanticDomain(after.Id) ?? throw new NullReferenceException($"unable to find semantic domain with id {after.Id}");
    }

    public Task DeleteSemanticDomain(Guid id)
    {
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Delete Semantic Domain",
            "Revert delete",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                SemanticDomainRepository.GetObject(id).Delete();
            });
        return Task.CompletedTask;
    }

    internal ICmSemanticDomain? GetLcmSemanticDomain(Guid semanticDomainId)
    {
        SemanticDomainRepository.TryGetObject(semanticDomainId, out var semanticDomain);
        return semanticDomain;
    }

    public IAsyncEnumerable<ComplexFormType> GetComplexFormTypes()
    {
        return ComplexFormTypesFlattened.Select(ToComplexFormType).ToAsyncEnumerable();
    }

    public Task<ComplexFormType?> GetComplexFormType(Guid id)
    {
        var lexEntryType = ComplexFormTypesFlattened.SingleOrDefault(c => c.Guid == id);
        if (lexEntryType is null) return Task.FromResult<ComplexFormType?>(null);
        return Task.FromResult<ComplexFormType?>(ToComplexFormType(lexEntryType));
    }

    private ComplexFormType ToComplexFormType(ILexEntryType t)
    {
        return new ComplexFormType() { Id = t.Guid, Name = FromLcmMultiString(t.Name) };
    }

    public async Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType)
    {
        await validators.ValidateAndThrow(complexFormType);
        if (complexFormType.Id == default) complexFormType.Id = Guid.NewGuid();
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Create complex form type",
            "Remove complex form type",
            Cache.ActionHandlerAccessor,
            () =>
            {
                var lexComplexFormType = Cache.ServiceLocator
                    .GetInstance<ILexEntryTypeFactory>()
                    .Create(complexFormType.Id);
                ComplexFormTypes.PossibilitiesOS.Add(lexComplexFormType);
                UpdateLcmMultiString(lexComplexFormType.Name, complexFormType.Name);
            });
        return ToComplexFormType(ComplexFormTypesFlattened.Single(c => c.Guid == complexFormType.Id));
    }

    public Task<ComplexFormType> UpdateComplexFormType(Guid id, UpdateObjectInput<ComplexFormType> update)
    {
        var type = ComplexFormTypesFlattened.SingleOrDefault(c => c.Guid == id);
        if (type is null) throw new NullReferenceException($"unable to find complex form type with id {id}");
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Update Complex Form Type",
            "Revert Complex Form Type",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                var updateProxy = new UpdateComplexFormTypeProxy(type, null, this);
                update.Apply(updateProxy);
            });
        return Task.FromResult(ToComplexFormType(type));
    }

    public async Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after)
    {
        await ComplexFormTypeSync.Sync(before, after, this);
        return ToComplexFormType(ComplexFormTypesFlattened.Single(c => c.Guid == after.Id));
    }

    public async Task DeleteComplexFormType(Guid id)
    {
        var type = ComplexFormTypesFlattened.SingleOrDefault(c => c.Guid == id);
        if (type is null) return;
        await Cache.DoUsingNewOrCurrentUOW("Delete Complex Form Type",
            "Revert delete",
            () =>
            {
                type.Delete();
                return ValueTask.CompletedTask;
            });
    }

    public IAsyncEnumerable<VariantType> GetVariantTypes()
    {
        return VariantTypes.PossibilitiesOS
            .Select(t => new VariantType() { Id = t.Guid, Name = FromLcmMultiString(t.Name) })
            .ToAsyncEnumerable();
    }

    private PartOfSpeech FromLcmPartOfSpeech(IPartOfSpeech lcmPos)
    {
        return new PartOfSpeech
        {
            Id = lcmPos.Guid,
            Name = FromLcmMultiString(lcmPos.Name),
            // TODO: Abreviation = FromLcmMultiString(partOfSpeech.Abreviation),
            Predefined = true, // NOTE: the !string.IsNullOrEmpty(lcmPos.CatalogSourceId) check doesn't work if the PoS originated in CRDT
        };
    }

    private Entry FromLexEntry(ILexEntry entry)
    {
        return new Entry
        {
            Id = entry.Guid,
            Note = FromLcmMultiString(entry.Comment),
            LexemeForm = FromLcmMultiString(entry.LexemeFormOA.Form),
            CitationForm = FromLcmMultiString(entry.CitationForm),
            LiteralMeaning = FromLcmMultiString(entry.LiteralMeaning),
            Senses = entry.AllSenses.Select(FromLexSense).ToList(),
            ComplexFormTypes = ToComplexFormTypes(entry),
            Components = ToComplexFormComponents(entry).ToList(),
            ComplexForms = [
                ..entry.ComplexFormEntries.Select(complexEntry => ToEntryReference(entry, complexEntry)),
                ..entry.AllSenses.SelectMany(sense => sense.ComplexFormEntries.Select(complexEntry => ToSenseReference(sense, complexEntry)))
            ]
        };
    }

    private string LexEntryHeadword(ILexEntry entry)
    {
        return new Entry()
        {
            LexemeForm = FromLcmMultiString(entry.LexemeFormOA.Form),
            CitationForm = FromLcmMultiString(entry.CitationForm),
        }.Headword();
    }

    private IList<ComplexFormType> ToComplexFormTypes(ILexEntry entry)
    {
        return entry.ComplexFormEntryRefs.SingleOrDefault()
            ?.ComplexEntryTypesRS
            .Select(ToComplexFormType)
            .ToList() ?? [];
    }
    private IEnumerable<ComplexFormComponent> ToComplexFormComponents(ILexEntry entry)
    {
        return entry.ComplexFormEntryRefs.SingleOrDefault()
            ?.ComponentLexemesRS
            .Select(o => o switch
            {
                ILexEntry component => ToEntryReference(component, entry),
                ILexSense s => ToSenseReference(s, entry),
                _ => throw new NotSupportedException($"object type {o.ClassName} not supported")
            }) ?? [];
    }

    private Variants? ToVariants(ILexEntry entry)
    {
        var variantEntryRef = entry.VariantEntryRefs.SingleOrDefault();
        if (variantEntryRef is null) return null;
        return new Variants
        {
            Id = variantEntryRef.Guid,
            VariantsOf =
            [
                ..variantEntryRef.ComponentLexemesRS.Select(o => o switch
                {
                    ILexEntry component => ToEntryReference(component, entry),
                    ILexSense s => ToSenseReference(s, entry),
                    _ => throw new NotSupportedException($"object type {o.ClassName} not supported")
                })
            ],
            Types =
            [
                ..variantEntryRef.VariantEntryTypesRS.Select(t =>
                    new VariantType() { Id = t.Guid, Name = FromLcmMultiString(t.Name), })
            ]
        };
    }

    private ComplexFormComponent ToEntryReference(ILexEntry component, ILexEntry complexEntry)
    {
        return new ComplexFormComponent
        {
            ComponentEntryId = component.Guid,
            ComponentHeadword = LexEntryHeadword(component),
            ComplexFormEntryId = complexEntry.Guid,
            ComplexFormHeadword = LexEntryHeadword(complexEntry)
        };
    }

    private ComplexFormComponent ToSenseReference(ILexSense componentSense, ILexEntry complexEntry)
    {
        return new ComplexFormComponent
        {
            ComponentEntryId = componentSense.Entry.Guid,
            ComponentSenseId = componentSense.Guid,
            ComponentHeadword = componentSense.Entry.HeadWord.Text,
            ComplexFormEntryId = complexEntry.Guid,
            ComplexFormHeadword = LexEntryHeadword(complexEntry)
        };
    }

    private Sense FromLexSense(ILexSense sense)
    {
        var enWs = GetWritingSystemHandle("en");
        var s =  new Sense
        {
            Id = sense.Guid,
            EntryId = sense.Entry.Guid,
            Gloss = FromLcmMultiString(sense.Gloss),
            Definition = FromLcmMultiString(sense.Definition),
            PartOfSpeech = sense.MorphoSyntaxAnalysisRA?.GetPartOfSpeech()?.Name.get_String(enWs).Text ?? "",
            PartOfSpeechId = sense.MorphoSyntaxAnalysisRA?.GetPartOfSpeech()?.Guid,
            SemanticDomains = sense.SemanticDomainsRC.Select(FromLcmSemanticDomain).ToList(),
            ExampleSentences = sense.ExamplesOS.Select(sentence => FromLexExampleSentence(sense.Guid, sentence)).ToList()
        };
        return s;
    }

    private ExampleSentence FromLexExampleSentence(Guid senseGuid, ILexExampleSentence sentence)
    {
        var translation = sentence.TranslationsOC.FirstOrDefault()?.Translation;
        return new ExampleSentence
        {
            Id = sentence.Guid,
            SenseId = senseGuid,
            Sentence = FromLcmMultiString(sentence.Example),
            Reference = sentence.Reference.Text,
            Translation = translation is null ? new MultiString() : FromLcmMultiString(translation),
        };
    }

    private MultiString FromLcmMultiString(ITsMultiString multiString)
    {
        var result = new MultiString(multiString.StringCount);
        for (var i = 0; i < multiString.StringCount; i++)
        {
            var tsString = multiString.GetStringFromIndex(i, out var ws);
            result.Values.Add(GetWritingSystemId(ws), tsString.Text);
        }

        return result;
    }

    public IAsyncEnumerable<Entry> GetEntries(QueryOptions? options = null)
    {
        return GetEntries(null, options);
    }

    public IAsyncEnumerable<Entry> GetEntries(
        Func<ILexEntry, bool>? predicate, QueryOptions? options = null)
    {
        var entries = EntriesRepository.AllInstances();

        options ??= QueryOptions.Default;
        if (predicate is not null) entries = entries.Where(predicate);

        if (options.Exemplar is not null)
        {
            var ws = GetWritingSystemHandle(options.Exemplar.WritingSystem, WritingSystemType.Vernacular);
            var exemplar = options.Exemplar.Value.Normalize(NormalizationForm.FormD);//LCM data is NFD so the should be as well
            entries = entries.Where(e =>
            {
                var value = (e.CitationForm.get_String(ws).Text ?? e.LexemeFormOA.Form.get_String(ws).Text)?
                    .Trim(LcmHelpers.WhitespaceAndFormattingChars);
                if (value is null || value.Length < exemplar.Length) return false;
                //exemplar is normalized, so we can use StartsWith
                //there may still be cases where value.StartsWith(value[0].ToString()) == false (e.g. "آبراهام")
                //another example is "เพือ" does not start with "เ"
                //but I don't have the data to test that
                return CultureInfo.InvariantCulture.CompareInfo.IsPrefix(value, exemplar, CompareOptions.IgnoreCase);
            });
        }

        var sortWs = GetWritingSystemHandle(options.Order.WritingSystem, WritingSystemType.Vernacular);
        entries = entries
            .OrderBy(e =>
            {
                string? text = e.CitationForm.get_String(sortWs).Text;
                text ??= e.LexemeFormOA.Form.get_String(sortWs).Text;
                return text?.Trim(LcmHelpers.WhitespaceChars);
            });
        entries = options.ApplyPaging(entries);

        return entries.ToAsyncEnumerable().Select(FromLexEntry);
    }

    public IAsyncEnumerable<Entry> SearchEntries(string query, QueryOptions? options = null)
    {
        var entries = GetEntries(e =>
            e.CitationForm.SearchValue(query) ||
            e.LexemeFormOA.Form.SearchValue(query) ||
            e.SensesOS.Any(s => s.Gloss.SearchValue(query)), options);
        return entries;
    }

    public Task<Entry?> GetEntry(Guid id)
    {
        return Task.FromResult<Entry?>(FromLexEntry(EntriesRepository.GetObject(id)));
    }

    public async Task<Entry> CreateEntry(Entry entry)
    {
        entry.Id = entry.Id == default ? Guid.NewGuid() : entry.Id;
        try
        {
            UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Create Entry",
                "Remove entry",
                Cache.ServiceLocator.ActionHandler,
                () =>
                {
                    var lexEntry = LexEntryFactory.Create(entry.Id,
                        Cache.ServiceLocator.GetInstance<ILangProjectRepository>().Singleton.LexDbOA);
                    lexEntry.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
                    UpdateLcmMultiString(lexEntry.LexemeFormOA.Form, entry.LexemeForm);
                    UpdateLcmMultiString(lexEntry.CitationForm, entry.CitationForm);
                    UpdateLcmMultiString(lexEntry.LiteralMeaning, entry.LiteralMeaning);
                    UpdateLcmMultiString(lexEntry.Comment, entry.Note);

                    foreach (var sense in entry.Senses)
                    {
                        CreateSense(lexEntry, sense);
                    }

                    //form types should be created before components, otherwise the form type "unspecified" will be added
                    foreach (var complexFormType in entry.ComplexFormTypes)
                    {
                        AddComplexFormType(lexEntry, complexFormType.Id);
                    }

                    foreach (var component in entry.Components)
                    {
                        AddComplexFormComponent(lexEntry, component);
                    }

                    foreach (var complexForm in entry.ComplexForms)
                    {
                        var complexLexEntry = EntriesRepository.GetObject(complexForm.ComplexFormEntryId);
                        AddComplexFormComponent(complexLexEntry, complexForm);
                    }
                });
        }
        catch (Exception e)
        {
            throw new CreateObjectException($"Failed to create entry {entry}", e);
        }

        return await GetEntry(entry.Id) ?? throw new InvalidOperationException("Entry was not created");
    }

    public Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Create Complex Form Component",
            "Remove Complex Form Component",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                var lexEntry = EntriesRepository.GetObject(complexFormComponent.ComplexFormEntryId);
                AddComplexFormComponent(lexEntry, complexFormComponent);
            });
        return Task.FromResult(ToComplexFormComponents(EntriesRepository.GetObject(complexFormComponent.ComplexFormEntryId))
            .Single(c => c.ComponentEntryId == complexFormComponent.ComponentEntryId && c.ComponentSenseId == complexFormComponent.ComponentSenseId));
    }

    public Task DeleteComplexFormComponent(ComplexFormComponent complexFormComponent)
    {
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Delete Complex Form Component",
            "Add Complex Form Component",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                var lexEntry = EntriesRepository.GetObject(complexFormComponent.ComplexFormEntryId);
                RemoveComplexFormComponent(lexEntry, complexFormComponent);
            });
        return Task.CompletedTask;
    }

    public Task AddComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Add Complex Form Type",
            "Remove Complex Form Type",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                AddComplexFormType(EntriesRepository.GetObject(entryId), complexFormTypeId);
            });
        return Task.CompletedTask;
    }

    public Task RemoveComplexFormType(Guid entryId, Guid complexFormTypeId)
    {
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Remove Complex Form Type",
            "Add Complex Form Type",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                RemoveComplexFormType(EntriesRepository.GetObject(entryId), complexFormTypeId);
            });
        return Task.CompletedTask;
    }

    /// <summary>
    /// must be called as part of an lcm action
    /// </summary>
    internal void AddComplexFormComponent(ILexEntry lexEntry, ComplexFormComponent component)
    {
        ICmObject lexComponent = component.ComponentSenseId is not null
            ? SenseRepository.GetObject(component.ComponentSenseId.Value)
            : EntriesRepository.GetObject(component.ComponentEntryId);
        lexEntry.AddComponent(lexComponent);
    }

    internal void RemoveComplexFormComponent(ILexEntry lexEntry, ComplexFormComponent component)
    {
        ICmObject lexComponent;
        if (component.ComponentSenseId is not null)
        {
            //sense has been deleted, so this complex form has been deleted already
            if (!SenseRepository.TryGetObject(component.ComponentSenseId.Value, out var sense)) return;
            lexComponent = sense;
        }
        else
        {
            //entry has been deleted, so this complex form has been deleted already
            if (!EntriesRepository.TryGetObject(component.ComponentEntryId, out var entry)) return;
            lexComponent = entry;
        }

        var entryRef = lexEntry.ComplexFormEntryRefs.Single();
        if (!entryRef.ComponentLexemesRS.Remove(lexComponent))
        {
            throw new InvalidOperationException("Complex form component not found, searched for " + lexComponent.ObjectIdName.Text);
        }
    }

    internal void AddComplexFormType(ILexEntry lexEntry, Guid complexFormTypeId)
    {
        ILexEntryRef? entryRef = lexEntry.ComplexFormEntryRefs.SingleOrDefault();
        if (entryRef is null)
        {
            entryRef = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
            lexEntry.EntryRefsOS.Add(entryRef);
            entryRef.RefType = LexEntryRefTags.krtComplexForm;
            entryRef.HideMinorEntry = 0;
        }

        var lexEntryType = ComplexFormTypesFlattened.Single(c => c.Guid == complexFormTypeId);
        entryRef.ComplexEntryTypesRS.Add(lexEntryType);
    }

    internal void RemoveComplexFormType(ILexEntry lexEntry, Guid complexFormTypeId)
    {
        ILexEntryRef? entryRef = lexEntry.ComplexFormEntryRefs.SingleOrDefault();
        if (entryRef is null) return;
        var lexEntryType = entryRef.ComplexEntryTypesRS.SingleOrDefault(c => c.Guid == complexFormTypeId);
        if (lexEntryType is null) return;
        entryRef.ComplexEntryTypesRS.Remove(lexEntryType);
    }

    private IList<ITsString> MultiStringToTsStrings(MultiString? multiString)
    {
        if (multiString is null) return [];
        var result = new List<ITsString>(multiString.Values.Count);
        foreach (var (ws, value) in multiString.Values)
        {
            result.Add(TsStringUtils.MakeString(value, GetWritingSystemHandle(ws)));
        }

        return result;
    }

    private void UpdateLcmMultiString(ITsMultiString multiString, MultiString newMultiString)
    {
        foreach (var (ws, value) in newMultiString.Values)
        {
            var writingSystemHandle = GetWritingSystemHandle(ws);
            multiString.set_String(writingSystemHandle, TsStringUtils.MakeString(value, writingSystemHandle));
        }
    }

    public Task<Entry> UpdateEntry(Guid id, UpdateObjectInput<Entry> update)
    {
        var lexEntry = EntriesRepository.GetObject(id);
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Update Entry",
            "Revert entry",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                var updateProxy = new UpdateEntryProxy(lexEntry, this);
                update.Apply(updateProxy);
            });
        return Task.FromResult(FromLexEntry(lexEntry));
    }

    public async Task<Entry> UpdateEntry(Entry before, Entry after)
    {
        await Cache.DoUsingNewOrCurrentUOW("Update Entry",
            "Revert entry",
            async () =>
            {
                await EntrySync.Sync(after, before, this);
            });
        return await GetEntry(after.Id) ?? throw new NullReferenceException("unable to find entry with id " + after.Id);
    }

    public Task DeleteEntry(Guid id)
    {
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Delete Entry",
            "Revert delete",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                EntriesRepository.GetObject(id).Delete();
            });
        return Task.CompletedTask;
    }

    internal void CreateSense(ILexEntry lexEntry, Sense sense)
    {
        var lexSense = LexSenseFactory.Create(sense.Id, lexEntry);
        var msa = new SandboxGenericMSA() { MsaType = lexSense.GetDesiredMsaType() };
        if (sense.PartOfSpeechId.HasValue && PartOfSpeechRepository.TryGetObject(sense.PartOfSpeechId.Value, out var pos))
        {
            msa.MainPOS = pos;
        }
        lexSense.SandboxMSA = msa;
        ApplySenseToLexSense(sense, lexSense);
    }

    private void ApplySenseToLexSense(Sense sense, ILexSense lexSense)
    {
        if (lexSense.MorphoSyntaxAnalysisRA.GetPartOfSpeech()?.Guid != sense.PartOfSpeechId)
        {
            IPartOfSpeech? pos = null;
            if (sense.PartOfSpeechId.HasValue)
            {
                PartOfSpeechRepository.TryGetObject(sense.PartOfSpeechId.Value, out pos);
            }
            lexSense.MorphoSyntaxAnalysisRA.SetMsaPartOfSpeech(pos);
        }
        UpdateLcmMultiString(lexSense.Gloss, sense.Gloss);
        UpdateLcmMultiString(lexSense.Definition, sense.Definition);
        foreach (var senseSemanticDomain in sense.SemanticDomains)
        {
            var lcmSemanticDomain = GetLcmSemanticDomain(senseSemanticDomain.Id);
            if (lcmSemanticDomain is not null) lexSense.SemanticDomainsRC.Add(lcmSemanticDomain);
        }

        foreach (var exampleSentence in sense.ExampleSentences)
        {
            CreateExampleSentence(lexSense, exampleSentence);
        }
    }

    public Task<Sense> CreateSense(Guid entryId, Sense sense)
    {
        if (sense.Id == default) sense.Id = Guid.NewGuid();
        if (!EntriesRepository.TryGetObject(entryId, out var lexEntry))
            throw new InvalidOperationException("Entry not found");
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Create Sense",
            "Remove sense",
            Cache.ServiceLocator.ActionHandler,
            () => CreateSense(lexEntry, sense));
        return Task.FromResult(FromLexSense(SenseRepository.GetObject(sense.Id)));
    }

    public Task<Sense> UpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update)
    {
        var lexSense = SenseRepository.GetObject(senseId);
        if (lexSense.Entry.Guid != entryId) throw new InvalidOperationException($"Sense {senseId} does not belong to the expected entry, expected Id {entryId}, actual Id {lexSense.Entry.Guid}");
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Update Sense",
            "Revert sense",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                var updateProxy = new UpdateSenseProxy(lexSense, this);
                update.Apply(updateProxy);
            });
        return Task.FromResult(FromLexSense(lexSense));
    }

    public Task AddSemanticDomainToSense(Guid senseId, SemanticDomain semanticDomain)
    {
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Add Semantic Domain to Sense",
            "Remove Semantic Domain from Sense",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                var lexSense = SenseRepository.GetObject(senseId);
                lexSense.SemanticDomainsRC.Add(GetLcmSemanticDomain(semanticDomain.Id));
            });
        return Task.CompletedTask;
    }

    public Task RemoveSemanticDomainFromSense(Guid senseId, Guid semanticDomainId)
    {
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Remove Semantic Domain from Sense",
            "Add Semantic Domain to Sense",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                var lexSense = SenseRepository.GetObject(senseId);
                lexSense.SemanticDomainsRC.Remove(lexSense.SemanticDomainsRC.First(sd => sd.Guid == semanticDomainId));
            });
        return Task.CompletedTask;
    }

    public Task DeleteSense(Guid entryId, Guid senseId)
    {
        var lexSense = SenseRepository.GetObject(senseId);
        if (lexSense.Entry.Guid != entryId) throw new InvalidOperationException("Sense does not belong to entry");
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Delete Sense",
            "Revert delete",
            Cache.ServiceLocator.ActionHandler,
            () => lexSense.Delete());
        return Task.CompletedTask;
    }

    public Task<ExampleSentence?> GetExampleSentence(Guid entryId, Guid senseId, Guid id)
    {
        var lcmExampleSentence = ExampleSentenceRepository.GetObject(id);
        return Task.FromResult(lcmExampleSentence is null ? null : FromLexExampleSentence(senseId, lcmExampleSentence));
    }

    internal void CreateExampleSentence(ILexSense lexSense, ExampleSentence exampleSentence)
    {
        var lexExampleSentence = LexExampleSentenceFactory.Create(exampleSentence.Id, lexSense);
        UpdateLcmMultiString(lexExampleSentence.Example, exampleSentence.Sentence);
        var freeTranslationType = CmPossibilityRepository.GetObject(CmPossibilityTags.kguidTranFreeTranslation);
        var translation = CmTranslationFactory.Create(lexExampleSentence, freeTranslationType);
        UpdateLcmMultiString(translation.Translation, exampleSentence.Translation);
        lexExampleSentence.Reference = TsStringUtils.MakeString(exampleSentence.Reference,
            lexExampleSentence.Reference.get_WritingSystem(0));
    }

    public Task<ExampleSentence> CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence)
    {
        if (exampleSentence.Id == default) exampleSentence.Id = Guid.NewGuid();
        if (!SenseRepository.TryGetObject(senseId, out var lexSense))
            throw new InvalidOperationException("Sense not found");
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Create Example Sentence",
            "Remove example sentence",
            Cache.ServiceLocator.ActionHandler,
            () => CreateExampleSentence(lexSense, exampleSentence));
        return Task.FromResult(FromLexExampleSentence(senseId, ExampleSentenceRepository.GetObject(exampleSentence.Id)));
    }

    public Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<ExampleSentence> update)
    {
        var lexExampleSentence = ExampleSentenceRepository.GetObject(exampleSentenceId);
        ValidateOwnership(lexExampleSentence, entryId, senseId);
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Update Example Sentence",
            "Revert example sentence",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                var updateProxy = new UpdateExampleSentenceProxy(lexExampleSentence, this);
                update.Apply(updateProxy);
            });
        return Task.FromResult(FromLexExampleSentence(senseId, lexExampleSentence));
    }

    public async Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        ExampleSentence before,
        ExampleSentence after)
    {
        await Cache.DoUsingNewOrCurrentUOW("Update Example Sentence",
            "Revert Example Sentence",
            async () =>
            {
                await ExampleSentenceSync.Sync(entryId, senseId, after, before, this);
            });
        return await GetExampleSentence(entryId, senseId, after.Id) ?? throw new NullReferenceException("unable to find example sentence with id " + after.Id);
    }

    public Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        var lexExampleSentence = ExampleSentenceRepository.GetObject(exampleSentenceId);
        ValidateOwnership(lexExampleSentence, entryId, senseId);
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Delete Example Sentence",
            "Revert delete",
            Cache.ServiceLocator.ActionHandler,
            () => lexExampleSentence.Delete());
        return Task.CompletedTask;
    }

    private static void ValidateOwnership(ILexExampleSentence lexExampleSentence, Guid entryId, Guid senseId)
    {
        //todo the owner many not be a sense, but could be something owned by the sense.
        if (lexExampleSentence.Owner is ILexSense sense)
        {
            if (sense.Guid != senseId) throw new InvalidOperationException("Example sentence does not belong to sense");
            if (sense.Entry.Guid != entryId)
                throw new InvalidOperationException("Example sentence does not belong to entry");
        }
        else
        {
            throw new InvalidOperationException("Example sentence does not belong to sense, it belongs to a " +
                                                lexExampleSentence.Owner.ClassName);
        }
    }
}
