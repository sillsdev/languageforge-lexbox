using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using FwDataMiniLcmBridge.Api.UpdateProxy;
using FwDataMiniLcmBridge.LcmUtils;
using Gridify;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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

public class FwDataMiniLcmApi(
    Lazy<LcmCache> cacheLazy,
    bool onCloseSave,
    ILogger<FwDataMiniLcmApi> logger,
    FwDataProject project,
    MiniLcmValidators validators,
    IOptions<FwDataBridgeConfig> config) : IMiniLcmApi, IMiniLcmSaveApi
{
    private FwDataBridgeConfig Config => config.Value;
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
    internal IEnumerable<ILexEntryType> ComplexFormTypesFlattened => ComplexFormTypes.PossibilitiesOS.Cast<ILexEntryType>().Flatten();

    private ICmPossibilityList VariantTypes => Cache.LangProject.LexDbOA.VariantEntryTypesOA;
    private ICmPossibilityList Publications => Cache.LangProject.LexDbOA.PublicationTypesOA;

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
        return Cache.GetWritingSystemId(ws);
    }

    internal int GetWritingSystemHandle(WritingSystemId ws, WritingSystemType? type = null)
    {
        return Cache.GetWritingSystemHandle(ws, type);
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

    public async Task<WritingSystem> GetWritingSystem(WritingSystemId id, WritingSystemType type)
    {
        var writingSystems = await GetWritingSystems();
        return type switch
        {
            WritingSystemType.Vernacular => writingSystems.Vernacular.FirstOrDefault(ws => ws.WsId == id),
            WritingSystemType.Analysis => writingSystems.Analysis.FirstOrDefault(ws => ws.WsId == id),
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        } ?? throw new NullReferenceException($"unable to find writing system with id {id}");
    }

    internal void CompleteExemplars(WritingSystems writingSystems)
    {
        var wsExemplars = writingSystems.Vernacular.Concat(writingSystems.Analysis)
            .DistinctBy(ws => ws.WsId)
            .ToDictionary(ws => ws, ws => ws.Exemplars.Select(s => s[0]).ToHashSet());
        var wsExemplarsByHandle = wsExemplars.ToFrozenDictionary(kv => GetWritingSystemHandle(kv.Key.WsId), kv => kv.Value);

        foreach (var entry in EntriesRepository.AllInstances())
        {
            if (entry.CitationForm is not null)
                LcmHelpers.ContributeExemplars(entry.CitationForm, wsExemplarsByHandle);
            if (entry.LexemeFormOA is {Form: not null })
                LcmHelpers.ContributeExemplars(entry.LexemeFormOA.Form, wsExemplarsByHandle);
        }

        foreach (var ws in wsExemplars.Keys)
        {
            ws.Exemplars = [.. wsExemplars[ws].Order().Select(s => s.ToString())];
        }
    }

    public async Task<WritingSystem> CreateWritingSystem(WritingSystemType type, WritingSystem writingSystem)
    {
        await validators.ValidateAndThrow(writingSystem);
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
        return FromLcmWritingSystem(ws, index, type);
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
                var updateProxy = new UpdateWritingSystemProxy(lcmWritingSystem)
                {
                    Id = Guid.Empty,
                    Type = type,
                };
                update.Apply(updateProxy);
            });
        return await GetWritingSystem(id, type);
    }

    public async Task<WritingSystem> UpdateWritingSystem(WritingSystem before, WritingSystem after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        await Cache.DoUsingNewOrCurrentUOW("Update WritingSystem",
            "Revert WritingSystem",
            async () =>
            {
                await WritingSystemSync.Sync(before, after, api ?? this);
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

    public async Task<PartOfSpeech> CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        await validators.ValidateAndThrow(partOfSpeech);
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
        return FromLcmPartOfSpeech(lcmPartOfSpeech ?? throw new InvalidOperationException("Part of speech was not created"));
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

    public async Task<PartOfSpeech> UpdatePartOfSpeech(PartOfSpeech before, PartOfSpeech after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        await PartOfSpeechSync.Sync(before, after, api ?? this);
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

    public async Task<Publication> CreatePublication(Publication pub)
    {
        ICmPossibility? lcmPublication = null;
        NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ServiceLocator.ActionHandler, () =>
            {
                lcmPublication = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>().Create(pub.Id, Cache.LangProject.LexDbOA.PublicationTypesOA);
                UpdateLcmMultiString(lcmPublication.Name, pub.Name);
            }
        );
        return await Task.FromResult(FromLcmPossibility(lcmPublication ?? throw new InvalidOperationException("Failed to create publication")));
    }

	private Publication FromLcmPossibility(ICmPossibility lcmPossibility)
    {
		var possibility = new Publication
		{
			Id = lcmPossibility.Guid,
			Name = FromLcmMultiString(lcmPossibility.Name)
		};

		return possibility;
	}

    public Task<Publication> UpdatePublication(Guid id, UpdateObjectInput<Publication> update)
    {
        throw new NotImplementedException();
    }

    public Task<Publication> UpdatePublication(Publication before, Publication after, IMiniLcmApi? api = null)
    {
        throw new NotImplementedException();
    }

    public Task DeletePublication(Guid id)
    {
        NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(Cache.ServiceLocator.ActionHandler, () =>
            {
                Cache.ServiceLocator.GetObject(id).Delete();
            }
        );
        return Task.CompletedTask;
    }

    internal SemanticDomain FromLcmSemanticDomain(ICmSemanticDomain semanticDomain)
    {
        return new SemanticDomain
        {
            Id = semanticDomain.Guid,
            Name = FromLcmMultiString(semanticDomain.Name),
            Code = LcmHelpers.GetSemanticDomainCode(semanticDomain),
            Predefined = CanonicalGuidsSemanticDomain.CanonicalSemDomGuids.Contains(semanticDomain.Guid),
        };
    }

    public IAsyncEnumerable<SemanticDomain> GetSemanticDomains()
    {
        return
            SemanticDomainRepository
            .AllInstances()
            .OrderBy(LcmHelpers.GetSemanticDomainCode)
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
        await validators.ValidateAndThrow(semanticDomain);
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

    public async Task<SemanticDomain> UpdateSemanticDomain(SemanticDomain before, SemanticDomain after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        await SemanticDomainSync.Sync(before, after, api ?? this);
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
                var updateProxy = new UpdateComplexFormTypeProxy(type, this);
                update.Apply(updateProxy);
            });
        return Task.FromResult(ToComplexFormType(type));
    }

    public async Task<ComplexFormType> UpdateComplexFormType(ComplexFormType before, ComplexFormType after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        await ComplexFormTypeSync.Sync(before, after, api ?? this);
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
    public IAsyncEnumerable<Publication> GetPublications()
    {
        return Publications.PossibilitiesOS
            .Select(FromLcmPossibility)
            .ToAsyncEnumerable();
    }
    public Task<Publication?> GetPublication(Guid id)
    {
        var publication = GetLcmPublication(id);
        return Task.FromResult(publication is null ? null : FromLcmPossibility(publication));
    }

    internal ICmPossibility? GetLcmPublication(Guid id)
    {
        return Publications.PossibilitiesOS.FirstOrDefault(p => p.Guid == id);
    }

    private PartOfSpeech FromLcmPartOfSpeech(IPartOfSpeech lcmPos)
    {
        return new PartOfSpeech
        {
            Id = lcmPos.Guid,
            Name = FromLcmMultiString(lcmPos.Name),
            // TODO: Abreviation = FromLcmMultiString(partOfSpeech.Abreviation),
            Predefined = CanonicalGuidsPartOfSpeech.CanonicalPosGuids.Contains(lcmPos.Guid),
        };
    }

    private Entry FromLexEntry(ILexEntry entry)
    {
        try
        {
            return new Entry
            {
                Id = entry.Guid,
                Note = FromLcmMultiString(entry.Comment),
                LexemeForm = FromLcmMultiString(entry.LexemeFormOA?.Form),
                CitationForm = FromLcmMultiString(entry.CitationForm),
                LiteralMeaning = FromLcmMultiString(entry.LiteralMeaning),
                Senses = entry.AllSenses.Select(FromLexSense).ToList(),
                ComplexFormTypes = ToComplexFormTypes(entry),
                Components = ToComplexFormComponents(entry).ToList(),
                ComplexForms = [
                    ..entry.ComplexFormEntries.Select(complexEntry => ToEntryReference(entry, complexEntry)),
                    ..entry.AllSenses.SelectMany(sense => sense.ComplexFormEntries.Select(complexEntry => ToSenseReference(sense, complexEntry)))
                ],
                // Add all the possibilities in the project which are not excluded by the entry's DoNotPublishIn field
                PublishIn = Publications.PossibilitiesOS.Where(
                    p => entry.DoNotPublishInRC.All(ep => ep.Guid != p.Guid)).Select(FromLcmPossibility).ToList(),
            };
        }
        catch (Exception e)
        {
            var headword = LexEntryHeadword(entry);
            throw new InvalidOperationException($"Failed to map FW entry to MiniLCM entry '{headword}' ({entry.Guid})", e);
        }
    }

    private string LexEntryHeadword(ILexEntry entry)
    {
        try
        {
            return new Entry()
            {
                LexemeForm = FromLcmMultiString(entry.LexemeFormOA?.Form),
                CitationForm = FromLcmMultiString(entry.CitationForm),
            }.Headword();
        }
        catch (Exception e)
        {
            throw new InvalidOperationException($"Failed to get headword for FW entry {entry.Guid}", e);
        }
    }

    private IList<ComplexFormType> ToComplexFormTypes(ILexEntry entry)
    {
        return entry.ComplexFormEntryRefs
            .SelectMany(r => r.ComplexEntryTypesRS, (_, type) => ToComplexFormType(type))
            .DistinctBy(c => c.Id)
            .ToList();
    }
    private IEnumerable<ComplexFormComponent> ToComplexFormComponents(ILexEntry entry)
    {
        return entry.ComplexFormEntryRefs.SelectMany(r => r.ComponentLexemesRS,
            (_, o) => o switch
            {
                ILexEntry component => ToEntryReference(component, entry),
                ILexSense s => ToSenseReference(s, entry),
                _ => throw new NotSupportedException($"object type {o.ClassName} not supported")
            }).DistinctBy(c => (c.ComponentEntryId, c.ComplexFormEntryId, c.ComponentSenseId));
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
        var pos = sense.MorphoSyntaxAnalysisRA?.GetPartOfSpeech();
        var s =  new Sense
        {
            Id = sense.Guid,
            EntryId = sense.Entry.Guid,
            Gloss = FromLcmMultiString(sense.Gloss),
            Definition = FromLcmMultiString(sense.Definition),
            PartOfSpeech = pos is null ? null : FromLcmPartOfSpeech(pos),
            PartOfSpeechId = pos?.Guid,
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
            Translation = translation is null ? new() : FromLcmMultiString(translation),
        };
    }

    private MultiString FromLcmMultiString(ITsMultiString? multiString)
    {
        if (multiString is null) return new MultiString();
        var result = new MultiString(multiString.StringCount);
        for (var i = 0; i < multiString.StringCount; i++)
        {
            var tsString = multiString.GetStringFromIndex(i, out var ws);
            result.Values.Add(GetWritingSystemId(ws), tsString.Text);
        }

        return result;
    }

    private RichMultiString FromLcmMultiString(IMultiString multiString)
    {
        var result = new RichMultiString(multiString.StringCount);
        for (var i = 0; i < multiString.StringCount; i++)
        {
            var tsString = multiString.GetStringFromIndex(i, out var ws);

            result.Add(GetWritingSystemId(ws), tsString.Text);
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
        if (!string.IsNullOrEmpty(options.Filter?.GridifyFilter))
        {
            var query = new GridifyQuery(){Filter = options.Filter.GridifyFilter};
            var filter = query.GetFilteringExpression(config.Value.Mapper).Compile();
            entries = entries.Where(filter);
        }

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
        string? order(ILexEntry e)
        {
            string? text = e.CitationForm.get_String(sortWs).Text;
            text ??= e.LexemeFormOA.Form.get_String(sortWs).Text;
            return text?.Trim(LcmHelpers.WhitespaceChars);
        }
        if (options.Order.Ascending)
        {
            entries = entries.OrderBy(order);
        } else
        {
            entries = entries.OrderByDescending(order);
        }
        entries = options.ApplyPaging(entries);

        return entries.ToAsyncEnumerable().Select(FromLexEntry);
    }

    public IAsyncEnumerable<Entry> SearchEntries(string query, QueryOptions? options = null)
    {
        var entries = GetEntries(e =>
            e.CitationForm.SearchValue(query) ||
            e.LexemeFormOA.Form.SearchValue(query) ||
            e.AllSenses.Any(s => s.Gloss.SearchValue(query)), options);
        return entries;
    }

    public Task<Entry?> GetEntry(Guid id)
    {
        return Task.FromResult<Entry?>(FromLexEntry(EntriesRepository.GetObject(id)));
    }

    public async Task<Entry> CreateEntry(Entry entry)
    {
        entry.Id = entry.Id == default ? Guid.NewGuid() : entry.Id;
        await validators.ValidateAndThrow(entry);
        try
        {
            UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Create Entry",
                "Remove entry",
                Cache.ServiceLocator.ActionHandler,
                () =>
                {
                    var lexEntry = LexEntryFactory.Create(entry.Id,
                        Cache.ServiceLocator.GetInstance<ILangProjectRepository>().Singleton.LexDbOA);
                    lexEntry.LexemeFormOA = Cache.CreateLexemeForm();
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
                    // Subtract entry.Publications from Publications to get the publications that the entry should not be published in
                    var doNotPublishIn = Publications.PossibilitiesOS.Where(p => entry.PublishIn.All(ep => ep.Id != p.Guid));
                    foreach (var publication in doNotPublishIn)
                    {
                        lexEntry.DoNotPublishInRC.Add(publication);
                    }
                });
        }
        catch (Exception e)
        {
            throw new CreateObjectException($"Failed to create entry {entry}", e);
        }

        return await GetEntry(entry.Id) ?? throw new InvalidOperationException("Entry was not created");
    }

    public Task<ComplexFormComponent> CreateComplexFormComponent(ComplexFormComponent complexFormComponent, BetweenPosition<ComplexFormComponent>? position = null)
    {
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Create Complex Form Component",
            "Remove Complex Form Component",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                var lexEntry = EntriesRepository.GetObject(complexFormComponent.ComplexFormEntryId);
                AddComplexFormComponent(lexEntry, complexFormComponent, position);
            });
        return Task.FromResult(ToComplexFormComponents(EntriesRepository.GetObject(complexFormComponent.ComplexFormEntryId))
            .Single(c => c.ComponentEntryId == complexFormComponent.ComponentEntryId && c.ComponentSenseId == complexFormComponent.ComponentSenseId));
    }

    public Task MoveComplexFormComponent(ComplexFormComponent component, BetweenPosition<ComplexFormComponent> between)
    {
        if (!EntriesRepository.TryGetObject(component.ComplexFormEntryId, out var lexComplexFormEntry))
            throw new InvalidOperationException("Entry not found");

        var lexComponent = FindSenseOrEntryComponent(component);

        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Move Complex Form Component",
            "Move Complex Form Component back",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                InsertComplexFormComponent(lexComplexFormEntry, lexComponent, between);
            });
        return Task.CompletedTask;
    }

    private ICmObject FindSenseOrEntryComponent(ComplexFormComponent component)
    {
        return component.ComponentSenseId is not null
            ? SenseRepository.GetObject(component.ComponentSenseId.Value)
            : EntriesRepository.GetObject(component.ComponentEntryId);
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
    internal void AddComplexFormComponent(ILexEntry lexComplexForm, ComplexFormComponent component, BetweenPosition<ComplexFormComponent>? between = null)
    {
        var lexComponent = FindSenseOrEntryComponent(component);
        InsertComplexFormComponent(lexComplexForm, lexComponent, between);
    }

    internal void InsertComplexFormComponent(ILexEntry lexComplexForm, ICmObject lexComponent, BetweenPosition<ComplexFormComponent>? between = null)
    {
        var previousComponentId = between?.Previous?.ComponentSenseId ?? between?.Previous?.ComponentEntryId;
        var nextComponentId = between?.Next?.ComponentSenseId ?? between?.Next?.ComponentEntryId;
        //there could be multiple valid refs, but we have no way of selecting which one to use so just use the first as that's what LCM does
        //we want to prefer the component which has the same guid as the previous or next component, if it exists
        var entryRef = lexComplexForm.ComplexFormEntryRefs
            .FirstOrDefault(entryRef => entryRef.ComponentLexemesRS.Any(o => o.Guid == previousComponentId || o.Guid == nextComponentId))
            ?? lexComplexForm.ComplexFormEntryRefs.FirstOrDefault();
        if (entryRef is null || entryRef.ComponentLexemesRS.Count == 0)
        {
            lexComplexForm.AddComponent(lexComponent);
            return;
        }


        // Prevents adding duplicates (which ComponentLexemesRS.Insert is susceptible to)
        if (entryRef.ComponentLexemesRS.Contains(lexComponent))
        {
            if (previousComponentId is null && nextComponentId is null) return;
            entryRef.ComponentLexemesRS.Remove(lexComponent);
        }

        var previousComponent = entryRef.ComponentLexemesRS.FirstOrDefault(s => s.Guid == previousComponentId);
        if (previousComponent is not null)
        {
            var insertI = entryRef.ComponentLexemesRS.IndexOf(previousComponent) + 1;
            if (insertI >= entryRef.ComponentLexemesRS.Count)
            {
                // Prefer AddComponent as it does some extra magical stuff 🤷
                lexComplexForm.AddComponent(lexComponent);
            }
            else
            {
                entryRef.ComponentLexemesRS.Insert(insertI, lexComponent);
            }
            return;
        }

        var nextComponent = entryRef.ComponentLexemesRS.FirstOrDefault(s => s.Guid == nextComponentId);
        if (nextComponent is not null)
        {
            var insertI = entryRef.ComponentLexemesRS.IndexOf(nextComponent);
            entryRef.ComponentLexemesRS.Insert(insertI, lexComponent);
            return;
        }

        lexComplexForm.AddComponent(lexComponent);
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

        foreach (var entryRef in lexEntry.ComplexFormEntryRefs)
        {
            entryRef.ComponentLexemesRS.Remove(lexComponent);
        }
        //not throwing to match CRDT behavior
    }

    internal void AddComplexFormType(ILexEntry lexEntry, Guid complexFormTypeId)
    {
        //do the same thing as LCM, use the first when adding if there's more than one
        ILexEntryRef? entryRef = lexEntry.ComplexFormEntryRefs.FirstOrDefault();
        if (entryRef is null)
        {
            entryRef = AddComplexFormLexEntryRef(lexEntry);
        }

        var lexEntryType = ComplexFormTypesFlattened.Single(c => c.Guid == complexFormTypeId);
        entryRef.ComplexEntryTypesRS.Add(lexEntryType);
    }

    internal ILexEntryRef AddComplexFormLexEntryRef(ILexEntry lexEntry)
    {
        var entryRef = Cache.ServiceLocator.GetInstance<ILexEntryRefFactory>().Create();
        lexEntry.EntryRefsOS.Add(entryRef);
        entryRef.RefType = LexEntryRefTags.krtComplexForm;
        entryRef.HideMinorEntry = 0;
        return entryRef;
    }

    internal void RemoveComplexFormType(ILexEntry lexEntry, Guid complexFormTypeId)
    {
        foreach (var entryRef in lexEntry.ComplexFormEntryRefs)
        {
            var lexEntryType = entryRef.ComplexEntryTypesRS.SingleOrDefault(c => c.Guid == complexFormTypeId);
            if (lexEntryType is null) continue;
            entryRef.ComplexEntryTypesRS.Remove(lexEntryType);
        }
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

    public Task AddPublication(Guid entryId, Guid publicationId)
    {
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Add Publication",
            "Remove Publication",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                AddPublication(EntriesRepository.GetObject(entryId), publicationId);
            });
        return Task.CompletedTask;
    }

    internal void AddPublication(ILexEntry entry, Guid publicationId)
    {
        var lcmPublication = GetLcmPublication(publicationId);
        if (lcmPublication is null) throw new NotFoundException("unable to find publication with id " + publicationId, nameof(Publication));
        entry.DoNotPublishInRC.Remove(lcmPublication);
    }

    public Task RemovePublication(Guid entryId, Guid publicationId)
    {
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Remove Publication",
            "Add Publication",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                RemovePublication(EntriesRepository.GetObject(entryId), publicationId);
            });
        return Task.CompletedTask;
    }

    internal void RemovePublication(ILexEntry entry, Guid publicationId)
    {
        var lcmPublication = GetLcmPublication(publicationId);
        if (lcmPublication is null)
            throw new NotFoundException("unable to find publication with id " + publicationId, nameof(Publication));
        entry.DoNotPublishInRC.Add(lcmPublication);
    }

    private void UpdateLcmMultiString(ITsMultiString multiString, MultiString newMultiString)
    {
        foreach (var (ws, value) in newMultiString.Values)
        {
            var writingSystemHandle = GetWritingSystemHandle(ws);
            multiString.set_String(writingSystemHandle, TsStringUtils.MakeString(value, writingSystemHandle));
        }
    }

    private void UpdateLcmMultiString(ITsMultiString multiString, RichMultiString newMultiString)
    {
        foreach (var (ws, value) in newMultiString)
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

    public async Task<Entry> UpdateEntry(Entry before, Entry after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        await Cache.DoUsingNewOrCurrentUOW("Update Entry",
            "Revert entry",
            async () =>
            {
                await EntrySync.Sync(before, after, api ?? this);
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

    internal void CreateSense(ILexEntry lexEntry, Sense sense, BetweenPosition? between = null)
    {
        var lexSense = LexSenseFactory.Create(sense.Id);
        InsertSense(lexEntry, lexSense, between);
        var msa = new SandboxGenericMSA() { MsaType = lexSense.GetDesiredMsaType() };
        if (sense.PartOfSpeechId.HasValue)
        {
            var found = PartOfSpeechRepository.TryGetObject(sense.PartOfSpeechId.Value, out var pos);
            if (!found) throw new InvalidOperationException($"Part of speech must exist when creating a sense (could not find GUID {sense.PartOfSpeechId.Value})");
            msa.MainPOS = pos;
        }
        lexSense.SandboxMSA = msa;
        ApplySenseToLexSense(sense, lexSense);
    }

    internal void InsertSense(ILexEntry lexEntry, ILexSense lexSense, BetweenPosition? between = null)
    {
        var previousSenseId = between?.Previous;
        var nextSenseId = between?.Next;

        var previousSense = previousSenseId.HasValue ? lexEntry.AllSenses.Find(s => s.Guid == previousSenseId) : null;
        if (previousSense is not null)
        {
            if (previousSense.SensesOS.Count > 0)
            {
                // if the sense has sub-senses, our sense will only come directly after it if it is the first sub-sense
                // ILcmOwningSequence treats an insert as a move if the item is already in it
                previousSense.SensesOS.Insert(0, lexSense);
            }
            else
            {
                // todo the user might have wanted it to be a subsense of previousSense
                var allSiblings = previousSense.Owner == lexEntry ? lexEntry.SensesOS
                    : previousSense.Owner is ILexSense parentSense ? parentSense.SensesOS
                    : throw new InvalidOperationException("Sense parent is not a sense or the expected entry");
                var insertI = allSiblings.IndexOf(previousSense) + 1;
                // ILcmOwningSequence treats an insert as a move if the item is already in it
                lexEntry.SensesOS.Insert(insertI, lexSense);
            }
            return;
        }

        var nextSense = nextSenseId.HasValue ? lexEntry.AllSenses.Find(s => s.Guid == nextSenseId) : null;
        if (nextSense is not null)
        {
            // todo the user might have wanted it to be a subsense of whatever is before nextSense
            var allSiblings = nextSense.Owner == lexEntry ? lexEntry.SensesOS
                    : nextSense.Owner is ILexSense parentSense ? parentSense.SensesOS
                    : throw new InvalidOperationException("Sense parent is not a sense or the expected entry");
            var insertI = allSiblings.IndexOf(nextSense);
            // ILcmOwningSequence treats an insert as a move if the item is already in it
            lexEntry.SensesOS.Insert(insertI, lexSense);
            return;
        }

        lexEntry.SensesOS.Add(lexSense);
    }

    internal void InsertExampleSentence(ILexSense lexSense, ILexExampleSentence lexExample, BetweenPosition? between = null)
    {
        var previousExampleId = between?.Previous;
        var nextExampleId = between?.Next;

        var previousExample = previousExampleId.HasValue ? lexSense.ExamplesOS.FirstOrDefault(s => s.Guid == previousExampleId) : null;
        if (previousExample is not null)
        {
            var insertI = lexSense.ExamplesOS.IndexOf(previousExample) + 1;
            // ILcmOwningSequence treats an insert as a move if the item is already in it
            lexSense.ExamplesOS.Insert(insertI, lexExample);
            return;
        }

        var nextExample = nextExampleId.HasValue ? lexSense.ExamplesOS.FirstOrDefault(s => s.Guid == nextExampleId) : null;
        if (nextExample is not null)
        {
            var insertI = lexSense.ExamplesOS.IndexOf(nextExample);
            // ILcmOwningSequence treats an insert as a move if the item is already in it
            lexSense.ExamplesOS.Insert(insertI, lexExample);
            return;
        }

        lexSense.ExamplesOS.Add(lexExample);
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

    public Task<Sense?> GetSense(Guid entryId, Guid id)
    {
        var lcmSense = SenseRepository.GetObject(id);
        return Task.FromResult(lcmSense is null ? null : FromLexSense(lcmSense));
    }

    public async Task<Sense> CreateSense(Guid entryId, Sense sense, BetweenPosition? between = null)
    {
        if (sense.Id == default) sense.Id = Guid.NewGuid();
        if (!EntriesRepository.TryGetObject(entryId, out var lexEntry))
            throw new InvalidOperationException("Entry not found");
        await validators.ValidateAndThrow(sense);
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Create Sense",
            "Remove sense",
            Cache.ServiceLocator.ActionHandler,
            () => CreateSense(lexEntry, sense, between));
        return FromLexSense(SenseRepository.GetObject(sense.Id));
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

    public async Task<Sense> UpdateSense(Guid entryId, Sense before, Sense after, IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        await Cache.DoUsingNewOrCurrentUOW("Update Sense",
            "Revert Sense",
            async () =>
            {
                await SenseSync.Sync(entryId, before, after, api ?? this);
            });
        return await GetSense(entryId, after.Id) ?? throw new NullReferenceException("unable to find sense with id " + after.Id);
    }

    public Task MoveSense(Guid entryId, Guid senseId, BetweenPosition between)
    {
        if (!EntriesRepository.TryGetObject(entryId, out var lexEntry))
            throw new InvalidOperationException("Entry not found");
        if (!SenseRepository.TryGetObject(senseId, out var lexSense))
            throw new InvalidOperationException("Sense not found");

        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Move Sense",
            "Move Sense back",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                InsertSense(lexEntry, lexSense, between);
            });
        return Task.CompletedTask;
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

    internal void CreateExampleSentence(ILexSense lexSense, ExampleSentence exampleSentence, BetweenPosition? between = null)
    {
        var lexExampleSentence = LexExampleSentenceFactory.Create(exampleSentence.Id);
        InsertExampleSentence(lexSense, lexExampleSentence, between);
        UpdateLcmMultiString(lexExampleSentence.Example, exampleSentence.Sentence);
        var freeTranslationType = CmPossibilityRepository.GetObject(CmPossibilityTags.kguidTranFreeTranslation);
        var translation = CmTranslationFactory.Create(lexExampleSentence, freeTranslationType);
        UpdateLcmMultiString(translation.Translation, exampleSentence.Translation);
        lexExampleSentence.Reference = TsStringUtils.MakeString(exampleSentence.Reference,
            lexExampleSentence.Reference.get_WritingSystem(0));
    }

    public ICmTranslation CreateExampleSentenceTranslation(ILexExampleSentence parent)
    {
        var freeTranslationType = CmPossibilityRepository.GetObject(CmPossibilityTags.kguidTranFreeTranslation);
        return CmTranslationFactory.Create(parent, freeTranslationType);
    }

    public async Task<ExampleSentence> CreateExampleSentence(Guid entryId, Guid senseId, ExampleSentence exampleSentence, BetweenPosition? between = null)
    {
        if (exampleSentence.Id == default) exampleSentence.Id = Guid.NewGuid();
        if (!SenseRepository.TryGetObject(senseId, out var lexSense))
            throw new InvalidOperationException("Sense not found");
        await validators.ValidateAndThrow(exampleSentence);
        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Create Example Sentence",
            "Remove example sentence",
            Cache.ServiceLocator.ActionHandler,
            () => CreateExampleSentence(lexSense, exampleSentence, between));
        return FromLexExampleSentence(senseId, ExampleSentenceRepository.GetObject(exampleSentence.Id));
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
        ExampleSentence after,
        IMiniLcmApi? api = null)
    {
        await validators.ValidateAndThrow(after);
        await Cache.DoUsingNewOrCurrentUOW("Update Example Sentence",
            "Revert Example Sentence",
            async () =>
            {
                await ExampleSentenceSync.Sync(entryId, senseId, before, after, api ?? this);
            });
        return await GetExampleSentence(entryId, senseId, after.Id) ?? throw new NullReferenceException("unable to find example sentence with id " + after.Id);
    }

    public Task MoveExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId, BetweenPosition between)
    {
        if (!EntriesRepository.TryGetObject(entryId, out var lexEntry))
            throw new InvalidOperationException("Entry not found");
        if (!SenseRepository.TryGetObject(senseId, out var lexSense))
            throw new InvalidOperationException("Sense not found");
        if (!ExampleSentenceRepository.TryGetObject(exampleSentenceId, out var lexExample))
            throw new InvalidOperationException("Example sentence not found");

        ValidateOwnership(lexExample, entryId, senseId);

        UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Move Example sentence",
            "Move Example sentence back",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                InsertExampleSentence(lexSense, lexExample, between);
            });
        return Task.CompletedTask;
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
