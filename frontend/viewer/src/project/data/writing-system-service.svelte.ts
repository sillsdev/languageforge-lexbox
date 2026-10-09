import type {
  IEntry,
  IExampleSentence,
  IMultiString,
  IRichMultiString,
  IRichString,
  ISense,
  IViewWritingSystem,
  IWritingSystem,
  IWritingSystems
} from '$lib/dotnet-types';
import {firstTruthy} from '$lib/utils';
import {type ProjectContext, useProjectContext} from '$project/project-context.svelte';
import {type ResourceReturn} from 'runed';
import type {View} from '$lib/views/view-data';
import type {ReadonlyDeep} from 'type-fest';
import {type MorphTypesService, useMorphTypesService} from './morph-types.svelte';

export type WritingSystemSelection =
  | 'vernacular'
  | 'analysis'
  | 'vernacular-no-audio'
  | 'analysis-no-audio'
  | 'first-vernacular'
  | 'first-analysis'
  | 'vernacular-analysis'
  | 'analysis-vernacular';
const symbol = Symbol.for('fw-lite-ws-service');
export function useWritingSystemService(): WritingSystemService {
  const projectContext = useProjectContext();
  const morphTypesService = useMorphTypesService();
  return projectContext.getOrAdd(symbol, () => new WritingSystemService(projectContext, morphTypesService));
}

export class WritingSystemService {

  private wsColors: WritingSystemColors = $derived(calcWritingSystemColors(this.writingSystems));
  #wsResource: ResourceReturn<IWritingSystems, unknown, true>;
  private get writingSystems(): IWritingSystems {
    return this.#wsResource.current;
  }

  #morphTypesService: MorphTypesService;

  constructor(projectContext: ProjectContext, morphTypesService: MorphTypesService) {
    this.#morphTypesService = morphTypesService;
    this.#wsResource = projectContext.apiResource({analysis: [], vernacular: []}, async api => {
      const result = await api.getWritingSystems();
      return {
        vernacular: result.vernacular,
        analysis: result.analysis
      };
    });
  }

  allWritingSystems(selection: Extract<WritingSystemSelection, 'vernacular-analysis' | 'analysis-vernacular'> = 'vernacular-analysis'): IWritingSystem[] {
    return this.pickWritingSystems(selection);
  }

  // Like allWritingSystems, but with duplicates removed. A writing system can belong to both the
  // vernacular and analysis lists (common in FieldWorks projects), so the concatenated list can
  // contain the same wsId twice; this keeps only the first occurrence of each.
  uniqueWritingSystems(selection: Extract<WritingSystemSelection, 'vernacular-analysis' | 'analysis-vernacular'> = 'vernacular-analysis'): IWritingSystem[] {
    return this.allWritingSystems(selection)
      .filter((ws, index, all) => all.findIndex((other) => other.wsId === ws.wsId) === index);
  }

  get analysis(): IWritingSystem[] {
    return this.pickWritingSystems('analysis');
  }

  get vernacular(): IWritingSystem[] {
    return this.pickWritingSystems('vernacular');
  }

  get analysisNoAudio(): IWritingSystem[] {
    return this.analysis.filter(ws => !ws.isAudio);
  }

  get vernacularNoAudio(): IWritingSystem[] {
    return this.vernacular.filter(ws => !ws.isAudio);
  }

  get defaultVernacular(): IWritingSystem | undefined {
    return this.writingSystems.vernacular[0];
  }

  get defaultAnalysis(): IWritingSystem | undefined {
    return this.writingSystems.analysis[0];
  }

  viewAnalysis(view: View) {
    return this.filterWs(this.analysis, view?.analysis);
  }

  viewVernacular(view: View) {
    return this.filterWs(this.vernacular, view?.vernacular);
  }

  /**
   * The view's non-audio vernacular writing systems, or all of them if the view only shows audio ones.
   * We currently only have fallback like this for vernacular, because headwords are the primary identifier in a lexicon,
   * so it can be highly disorienting if none is displayed
   * */
  viewVernacularNoAudio(view: View): IWritingSystem[] {
    const writingSystems = this.viewVernacular(view).filter(ws => !ws.isAudio);
    return writingSystems.length ? writingSystems : this.vernacularNoAudio;
  }

  filterWs(writingSystems: IWritingSystem[], override?: IViewWritingSystem[]) {
    if (!override) return writingSystems;
    return writingSystems.filter(ws => override.find(_ws => _ws.wsId === ws.wsId));
  }

  pickWritingSystems(
    ws?: WritingSystemSelection,
  ): IWritingSystem[] {
    ws = ws ?? 'vernacular-analysis';
    switch (ws) {
      case 'vernacular-analysis':
        return [...this.writingSystems.vernacular, ...this.writingSystems.analysis];
      case 'analysis-vernacular':
        return [...this.writingSystems.analysis, ...this.writingSystems.vernacular];
      case 'first-analysis':
        return [this.writingSystems.analysis[0]];
      case 'first-vernacular':
        return [this.writingSystems.vernacular[0]];
      case 'vernacular':
        return this.writingSystems.vernacular;
      case 'analysis':
        return this.writingSystems.analysis;
      case 'vernacular-no-audio':
        return this.vernacularNoAudio;
      case 'analysis-no-audio':
        return this.analysisNoAudio;
    }
    console.error(`Unknown writing system selection: ${ws as string}`);
    return [];
  }

  indexExemplars(): string[] | undefined {
    return this.defaultVernacular?.exemplars;
  }

  headword(entry: ReadonlyDeep<IEntry>, ws: string): string {
    return this.#decorated(entry, ws) || '';
  }

  firstHeadword(entry: ReadonlyDeep<IEntry>): string {
    return firstTruthy(this.vernacularNoAudio, ws => this.#decorated(entry, ws.wsId)) || '';
  }

  /**
   * Like {@link firstHeadword}, but prefers the view's vernaculars over the rest, so the simple list
   * and the dictionary preview agree on which headword an entry shows.
   */
  viewBestHeadword(entry: ReadonlyDeep<IEntry>, view: View, ws?: string): string {
    return this.viewBestHeadwordIn(entry, view, ws)?.value ?? '';
  }

  /** {@link viewBestHeadword} plus the writing system the headword came from, so a fallback can be labelled. */
  viewBestHeadwordIn(entry: ReadonlyDeep<IEntry>, view: View, wsId?: string): {ws: IWritingSystem, value: string} | undefined {
    const viewWs = this.viewVernacularNoAudio(view);
    const preferred = wsId ? this.getWritingSystem(wsId, 'vernacular') : viewWs[0];
    const candidates = [...(preferred ? [preferred] : []), ...viewWs, ...this.vernacularNoAudio];
    return firstTruthy(candidates, ws => {
      const value = this.headword(entry, ws.wsId);
      return value ? {ws, value} : undefined;
    });
  }

  #decorated(entry: ReadonlyDeep<IEntry>, ws: string): string | undefined {
    // Citation forms should not be decorated with prefix/postfix tokens, only lexeme forms get decorated
    return entry.citationForm[ws] || this.#morphTypesService.decorate(entry.lexemeForm[ws], entry.morphType);
  }

  pickBestAlternative(value: IMultiString, wss: 'vernacular' | 'analysis'): string
  pickBestAlternative(value: IMultiString, firstChoice: IWritingSystem): string
  pickBestAlternative(value: IMultiString, firstChoice: IWritingSystem | 'vernacular' | 'analysis'): string {
    let allWs: IWritingSystem[];
    if (typeof firstChoice === 'object') {
      allWs = [firstChoice, ...this.allWritingSystems()];
    } else {
      switch (firstChoice) {
        case 'vernacular':
          allWs = this.allWritingSystems('vernacular-analysis');
          break;
        case 'analysis':
          allWs = this.allWritingSystems('analysis-vernacular');
          break;
        default:
          throw new Error(`Unknown writing system selection ${firstChoice as unknown as string}`);
      }
    }

    return this.first(value, allWs) || '';
  }

  getWritingSystem(wsId: string, selection?: 'vernacular' | 'analysis'): IWritingSystem | undefined {
    const writingSystems = this.pickWritingSystems(selection);
    return writingSystems.find(ws => ws.wsId === wsId);
  }

  firstGloss(sense: ISense): string {
    return this.first(sense.gloss, this.analysis) || '';
  }

  firstDef(sense: ISense): string {
    return this.first(sense.definition, this.analysis) || '';
  }

  glosses(entry: IEntry | undefined): string {
    if (!entry?.senses?.length) return '';
    return entry.senses.map(sense => this.first(sense.gloss, this.analysis)).filter(gloss => !!gloss).join(', ');
  }

  firstDefOrGlossVal(sense: ISense | undefined): string {
    if (!sense) return '';
    return this.first(sense.definition, this.analysis) || this.first(sense.gloss, this.analysis) || '';
  }

  /** Like {@link firstDefOrGlossVal}, but prefers the view's analysis writing systems. */
  viewFirstDefOrGlossVal(sense: ISense | undefined, view: View): string {
    if (!sense) return '';
    const viewAnalysis = this.viewAnalysis(view).filter(ws => !ws.isAudio);
    return this.first(sense.definition, viewAnalysis)
      || this.first(sense.gloss, viewAnalysis)
      || this.firstDefOrGlossVal(sense);
  }

  firstSentenceOrTranslationVal(example: IExampleSentence | undefined): string {
    if (!example) return '';
    return this.first(example.sentence, this.vernacular) || this.firstTranslationVal(example);
  }

  firstTranslationVal(example: IExampleSentence | undefined): string {
    if (!example) return '';
    for (const translation of example.translations) {
      const text = this.first(translation.text, this.analysis);
      if (text) return text;
    }
    return '';
  }

  wsColor(ws: string, type: 'vernacular' | 'analysis'): string {
    const colors = this.wsColors[type];
    return colors[ws];
  }

  first(value: IMultiString | IRichMultiString, writingSystems: IWritingSystem[] = this.allWritingSystems()): string | undefined {
    return firstTruthy(writingSystems, ws => asString(value[ws.wsId]));
  }
}

export function asString(value: IRichString | string | undefined): string | undefined {
  if (!value || typeof value === 'string') return value;
  return value.spans.map(s => s.text).join('');
}

type WritingSystemColors = {
  vernacular: Record<string, typeof vernacularColors[number]>;
  analysis: Record<string, typeof analysisColors[number]>;
}

function calcWritingSystemColors(writingSystems: IWritingSystems): WritingSystemColors {
  const wsColors = {
    vernacular: {} as Record<string, typeof vernacularColors[number]>,
    analysis: {} as Record<string, typeof analysisColors[number]>,
  };
  writingSystems.vernacular.forEach((ws, i) => {
    wsColors.vernacular[ws.wsId] = vernacularColors[i % vernacularColors.length];
  });
  writingSystems.analysis.forEach((ws, i) => {
    wsColors.analysis[ws.wsId] = analysisColors[i % analysisColors.length];
  });
  return wsColors;
}

const vernacularColors = [
  'text-emerald-700 dark:text-emerald-300',
  'text-fuchsia-700 dark:text-fuchsia-300',
  'text-amber-700 dark:text-amber-300',
] as const;

const analysisColors = [
  'text-blue-700 dark:text-blue-300',
  'text-rose-700 dark:text-rose-400',
  'text-lime-700 dark:text-lime-300',
] as const;
