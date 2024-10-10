using System.Collections.Frozen;
using System.Reflection;
using System.Text;
using FwDataMiniLcmBridge.Api.UpdateProxy;
using Microsoft.Extensions.Logging;
using MiniLcm;
using MiniLcm.Models;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace FwDataMiniLcmBridge.Api;

public class FwDataMiniLcmApi(Lazy<LcmCache> cacheLazy, bool onCloseSave, ILogger<FwDataMiniLcmApi> logger, FwDataProject project) : IMiniLcmApi, IDisposable
{
    public LcmCache Cache => cacheLazy.Value;
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
            Vernacular = WritingSystemContainer.CurrentVernacularWritingSystems.Select(FromLcmWritingSystem).ToArray(),
            Analysis = Cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Select(FromLcmWritingSystem).ToArray()
        };
        CompleteExemplars(writingSystems);
        return Task.FromResult(writingSystems);
    }

    private WritingSystem FromLcmWritingSystem(CoreWritingSystemDefinition ws)
    {
        return new WritingSystem
        {
            //todo determine current and create a property for that.
            Id = ws.Id,
            Name = ws.LanguageTag,
            Abbreviation = ws.Abbreviation,
            Font = ws.DefaultFontName,
            Exemplars = ws.CharacterSets.FirstOrDefault(s => s.Type == "index")?.Characters.ToArray() ?? []
        };
    }

    internal void CompleteExemplars(WritingSystems writingSystems)
    {
        var wsExemplars = writingSystems.Vernacular.Concat(writingSystems.Analysis)
            .DistinctBy(ws => ws.Id)
            .ToDictionary(ws => ws, ws => ws.Exemplars.Select(s => s[0]).ToHashSet());
        var wsExemplarsByHandle = wsExemplars.ToFrozenDictionary(kv => GetWritingSystemHandle(kv.Key.Id), kv => kv.Value);

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
        CoreWritingSystemDefinition? ws = null;
        UndoableUnitOfWorkHelper.Do("Create Writing System",
            "Remove writing system",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                Cache.ServiceLocator.WritingSystemManager.GetOrSet(writingSystem.Id.Code, out ws);
                ws.Abbreviation = writingSystem.Abbreviation;
                switch (type)
                {
                    case WritingSystemType.Analysis:
                        Cache.ServiceLocator.WritingSystems.AddToCurrentAnalysisWritingSystems(ws);
                        break;
                    case WritingSystemType.Vernacular:
                        Cache.ServiceLocator.WritingSystems.AddToCurrentVernacularWritingSystems(ws);
                        break;
                }
            });
        if (ws is null) throw new InvalidOperationException("Writing system not found");
        return Task.FromResult(FromLcmWritingSystem(ws));
    }

    public Task<WritingSystem> UpdateWritingSystem(WritingSystemId id, WritingSystemType type, UpdateObjectInput<WritingSystem> update)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<PartOfSpeech> GetPartsOfSpeech()
    {
        return PartOfSpeechRepository
            .AllInstances()
            .OrderBy(p => p.Name.BestAnalysisAlternative.Text)
            .ToAsyncEnumerable()
            .Select(partOfSpeech => new PartOfSpeech
            {
                Id = partOfSpeech.Guid,
                Name = FromLcmMultiString(partOfSpeech.Name)
            });
    }

    public Task CreatePartOfSpeech(PartOfSpeech partOfSpeech)
    {
        if (partOfSpeech.Id == default) partOfSpeech.Id = Guid.NewGuid();
        UndoableUnitOfWorkHelper.Do("Create Part of Speech",
            "Remove part of speech",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                var lcmPartOfSpeech = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>()
                    .Create(partOfSpeech.Id, Cache.LangProject.PartsOfSpeechOA);
                UpdateLcmMultiString(lcmPartOfSpeech.Name, partOfSpeech.Name);
            });
        return Task.CompletedTask;
    }

    public IAsyncEnumerable<SemanticDomain> GetSemanticDomains()
    {
        return
            SemanticDomainRepository
            .AllInstances()
            .OrderBy(p => p.Abbreviation.UiString)
            .ToAsyncEnumerable()
            .Select(semanticDomain => new SemanticDomain
            {
                Id = semanticDomain.Guid,
                Name = FromLcmMultiString(semanticDomain.Name),
                Code = semanticDomain.Abbreviation.UiString ?? ""
            });
    }

    public Task CreateSemanticDomain(SemanticDomain semanticDomain)
    {
        if (semanticDomain.Id == Guid.Empty) semanticDomain.Id = Guid.NewGuid();
        UndoableUnitOfWorkHelper.Do("Create Semantic Domain",
            "Remove semantic domain",
            Cache.ActionHandlerAccessor,
            () =>
            {
                var lcmSemanticDomain = Cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>()
                    .Create(semanticDomain.Id, Cache.LangProject.SemanticDomainListOA);
                UpdateLcmMultiString(lcmSemanticDomain.Name, semanticDomain.Name);
                UpdateLcmMultiString(lcmSemanticDomain.Abbreviation, new MultiString(){{"en", semanticDomain.Code}});
            });
        return Task.CompletedTask;
    }

    internal ICmSemanticDomain GetLcmSemanticDomain(Guid semanticDomainId)
    {
        return SemanticDomainRepository.GetObject(semanticDomainId);
    }

    public IAsyncEnumerable<ComplexFormType> GetComplexFormTypes()
    {
        return ComplexFormTypes.PossibilitiesOS
            .Select(ToComplexFormType)
            .ToAsyncEnumerable();
    }

    private ComplexFormType ToComplexFormType(ICmPossibility t)
    {
        return new ComplexFormType() { Id = t.Guid, Name = FromLcmMultiString(t.Name) };
    }

    public Task<ComplexFormType> CreateComplexFormType(ComplexFormType complexFormType)
    {
        if (complexFormType.Id != default) throw new InvalidOperationException("Complex form type id must be empty");
        UndoableUnitOfWorkHelper.Do("Create complex form type",
            "Remove complex form type",
            Cache.ActionHandlerAccessor,
            () =>
            {
                var lexComplexFormType = Cache.ServiceLocator
                    .GetInstance<ILexEntryTypeFactory>()
                    .Create();
                ComplexFormTypes.PossibilitiesOS.Add(lexComplexFormType);
                UpdateLcmMultiString(lexComplexFormType.Name, complexFormType.Name);
                complexFormType.Id = lexComplexFormType.Guid;
            });
        return Task.FromResult(ToComplexFormType(ComplexFormTypes.PossibilitiesOS.Single(c => c.Guid == complexFormType.Id)));
    }

    public IAsyncEnumerable<VariantType> GetVariantTypes()
    {
        return VariantTypes.PossibilitiesOS
            .Select(t => new VariantType() { Id = t.Guid, Name = FromLcmMultiString(t.Name) })
            .ToAsyncEnumerable();
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
            ComplexFormTypes = ToComplexFormType(entry),
            Components = ToComplexFormComponents(entry),
            //todo, this does not include complex forms which reference a sense
            ComplexForms = [..entry.ComplexFormEntries.Select(complexEntry => ToEntryReference(entry, complexEntry))]
        };
    }

    private IList<ComplexFormType> ToComplexFormType(ILexEntry entry)
    {
        return entry.ComplexFormEntryRefs.SingleOrDefault()
            ?.ComplexEntryTypesRS
            .Select(ToComplexFormType)
            .ToList() ?? [];
    }
    private IList<ComplexFormComponent> ToComplexFormComponents(ILexEntry entry)
    {
        return entry.ComplexFormEntryRefs.SingleOrDefault()
            ?.ComponentLexemesRS
            .Select(o => o switch
            {
                ILexEntry component => ToEntryReference(component, entry),
                ILexSense s => ToSenseReference(s, entry),
                _ => throw new NotSupportedException($"object type {o.ClassName} not supported")
            })
            .ToList() ?? [];
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
            ComponentHeadword = component.HeadWord.Text,
            ComplexFormEntryId = complexEntry.Guid,
            ComplexFormHeadword = complexEntry.HeadWord.Text
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
            ComplexFormHeadword = complexEntry.HeadWord.Text
        };
    }

    private Sense FromLexSense(ILexSense sense)
    {
        var enWs = GetWritingSystemHandle("en");
        var s =  new Sense
        {
            Id = sense.Guid,
            Gloss = FromLcmMultiString(sense.Gloss),
            Definition = FromLcmMultiString(sense.Definition),
            PartOfSpeech = sense.MorphoSyntaxAnalysisRA?.GetPartOfSpeech()?.Name.get_String(enWs).Text ?? "",
            PartOfSpeechId = sense.MorphoSyntaxAnalysisRA?.GetPartOfSpeech()?.Guid,
            SemanticDomains = sense.SemanticDomainsRC.Select(s => new SemanticDomain
            {
                Id = s.Guid,
                Name = FromLcmMultiString(s.Name),
                Code = s.OcmCodes
            }).ToList(),
            ExampleSentences = sense.ExamplesOS.Select(FromLexExampleSentence).ToList()
        };
        return s;
    }

    private ExampleSentence FromLexExampleSentence(ILexExampleSentence sentence)
    {
        var translation = sentence.TranslationsOC.FirstOrDefault()?.Translation;
        return new ExampleSentence
        {
            Id = sentence.Guid,
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
                //but I don't have the data to test that
                return value.StartsWith(exemplar, StringComparison.InvariantCultureIgnoreCase);
            });
        }

        var sortWs = GetWritingSystemHandle(options.Order.WritingSystem, WritingSystemType.Vernacular);
        entries = entries
            .OrderBy(e =>
            {
                string? text = e.CitationForm.get_String(sortWs).Text;
                text ??= e.LexemeFormOA.Form.get_String(sortWs).Text;
                return text?.Trim(LcmHelpers.WhitespaceChars);
            })
            .Skip(options.Offset)
            .Take(options.Count);

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
        UndoableUnitOfWorkHelper.Do("Create Entry",
            "Remove entry",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                var lexEntry = LexEntryFactory.Create(entry.Id, Cache.ServiceLocator.GetInstance<ILangProjectRepository>().Singleton.LexDbOA);
                lexEntry.LexemeFormOA = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
                UpdateLcmMultiString(lexEntry.LexemeFormOA.Form, entry.LexemeForm);
                UpdateLcmMultiString(lexEntry.CitationForm, entry.CitationForm);
                UpdateLcmMultiString(lexEntry.LiteralMeaning, entry.LiteralMeaning);
                UpdateLcmMultiString(lexEntry.Comment, entry.Note);

                foreach (var sense in entry.Senses)
                {
                    CreateSense(lexEntry, sense);
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

                foreach (var complexFormType in entry.ComplexFormTypes)
                {
                    AddComplexFormType(lexEntry, complexFormType.Id);
                }
            });

        return await GetEntry(entry.Id) ?? throw new InvalidOperationException("Entry was not created");
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
        ICmObject lexComponent = component.ComponentSenseId is not null
            ? SenseRepository.GetObject(component.ComponentSenseId.Value)
            : EntriesRepository.GetObject(component.ComponentEntryId);
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

        var lexEntryType = (ILexEntryType)ComplexFormTypes.PossibilitiesOS.Single(c => c.Guid == complexFormTypeId);
        entryRef.ComplexEntryTypesRS.Add(lexEntryType);
    }

    internal void RemoveComplexFormType(ILexEntry lexEntry, Guid complexFormTypeId)
    {
        ILexEntryRef? entryRef = lexEntry.ComplexFormEntryRefs.SingleOrDefault();
        if (entryRef is null) return;
        var lexEntryType = (ILexEntryType)ComplexFormTypes.PossibilitiesOS.Single(c => c.Guid == complexFormTypeId);
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
        UndoableUnitOfWorkHelper.Do("Update Entry",
            "Revert entry",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                var updateProxy = new UpdateEntryProxy(lexEntry, this);
                update.Apply(updateProxy);
            });
        return Task.FromResult(FromLexEntry(lexEntry));
    }

    public Task DeleteEntry(Guid id)
    {
        UndoableUnitOfWorkHelper.Do("Delete Entry",
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
                pos = PartOfSpeechRepository.GetObject(sense.PartOfSpeechId.Value);
            }
            lexSense.MorphoSyntaxAnalysisRA.SetMsaPartOfSpeech(pos);
        }
        UpdateLcmMultiString(lexSense.Gloss, sense.Gloss);
        UpdateLcmMultiString(lexSense.Definition, sense.Definition);
        foreach (var senseSemanticDomain in sense.SemanticDomains)
        {
            lexSense.SemanticDomainsRC.Add(GetLcmSemanticDomain(senseSemanticDomain.Id));
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
        UndoableUnitOfWorkHelper.Do("Create Sense",
            "Remove sense",
            Cache.ServiceLocator.ActionHandler,
            () => CreateSense(lexEntry, sense));
        return Task.FromResult(FromLexSense(SenseRepository.GetObject(sense.Id)));
    }

    public Task<Sense> UpdateSense(Guid entryId, Guid senseId, UpdateObjectInput<Sense> update)
    {
        var lexSense = SenseRepository.GetObject(senseId);
        if (lexSense.Entry.Guid != entryId) throw new InvalidOperationException($"Sense {senseId} does not belong to the expected entry, expected Id {entryId}, actual Id {lexSense.Entry.Guid}");
        UndoableUnitOfWorkHelper.Do("Update Sense",
            "Revert sense",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                var updateProxy = new UpdateSenseProxy(lexSense, this);
                update.Apply(updateProxy);
            });
        return Task.FromResult(FromLexSense(lexSense));
    }

    public Task DeleteSense(Guid entryId, Guid senseId)
    {
        var lexSense = SenseRepository.GetObject(senseId);
        if (lexSense.Entry.Guid != entryId) throw new InvalidOperationException("Sense does not belong to entry");
        UndoableUnitOfWorkHelper.Do("Delete Sense",
            "Revert delete",
            Cache.ServiceLocator.ActionHandler,
            () => lexSense.Delete());
        return Task.CompletedTask;
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
        UndoableUnitOfWorkHelper.Do("Create Example Sentence",
            "Remove example sentence",
            Cache.ServiceLocator.ActionHandler,
            () => CreateExampleSentence(lexSense, exampleSentence));
        return Task.FromResult(FromLexExampleSentence(ExampleSentenceRepository.GetObject(exampleSentence.Id)));
    }

    public Task<ExampleSentence> UpdateExampleSentence(Guid entryId,
        Guid senseId,
        Guid exampleSentenceId,
        UpdateObjectInput<ExampleSentence> update)
    {
        var lexExampleSentence = ExampleSentenceRepository.GetObject(exampleSentenceId);
        ValidateOwnership(lexExampleSentence, entryId, senseId);
        UndoableUnitOfWorkHelper.Do("Update Example Sentence",
            "Revert example sentence",
            Cache.ServiceLocator.ActionHandler,
            () =>
            {
                var updateProxy = new UpdateExampleSentenceProxy(lexExampleSentence, this);
                update.Apply(updateProxy);
            });
        return Task.FromResult(FromLexExampleSentence(lexExampleSentence));
    }

    public Task DeleteExampleSentence(Guid entryId, Guid senseId, Guid exampleSentenceId)
    {
        var lexExampleSentence = ExampleSentenceRepository.GetObject(exampleSentenceId);
        ValidateOwnership(lexExampleSentence, entryId, senseId);
        UndoableUnitOfWorkHelper.Do("Delete Example Sentence",
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
