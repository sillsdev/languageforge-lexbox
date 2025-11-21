import type {
  IEntry,
  IExampleSentence,
  IMultiString,
  IRichMultiString,
  IRichString,
  ISense,
  IWritingSystem,
  IWritingSystems
} from '$lib/dotnet-types';
import {firstTruthy} from '$lib/utils';
import {type ProjectContext, useProjectContext} from '$project/project-context.svelte';
import {type ResourceReturn} from 'runed';
import type {View} from '$lib/views/view-data';
import type {ReadonlyDeep} from 'type-fest';

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
  return projectContext.getOrAdd(symbol, () => new WritingSystemService(projectContext));
}

export class WritingSystemService {

  private wsColors: WritingSystemColors = $derived(calcWritingSystemColors(this.writingSystems));
  #wsResource: ResourceReturn<IWritingSystems, unknown, true>;
  private get writingSystems(): IWritingSystems {
    return this.#wsResource.current;
  }

  constructor(projectContext: ProjectContext) {
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
    return this.filterAndSortWs(this.analysis, view?.overrides?.analysisWritingSystems);
  }

  viewVernacular(view: View) {
    return this.filterAndSortWs(this.vernacular, view?.overrides?.vernacularWritingSystems);
  }

  filterAndSortWs(writingSystems: IWritingSystem[], override?: string[]) {
    if (!override) return writingSystems;
    return override
      .map(wsId => writingSystems.find(ws => ws.wsId === wsId))
      .filter(ws => !!ws);
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

  headword(entry: ReadonlyDeep<IEntry>, ws?: string): string {
    if (ws) {
      return headword(entry, ws) || '';
    }

    return firstTruthy(this.vernacularNoAudio, ws => headword(entry, ws.wsId)) || '';
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

function headword(entry: ReadonlyDeep<IEntry>, ws: string): string | undefined {
  return entry.citationForm[ws] || entry.lexemeForm[ws];
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
  'text-emerald-400 dark:text-emerald-300',
  'text-fuchsia-600 dark:text-fuchsia-300',
  'text-lime-600 dark:text-lime-200',
] as const;

const analysisColors = [
  'text-blue-500 dark:text-blue-300',
  'text-yellow-500 dark:text-yellow-200',
  'text-rose-500 dark:text-rose-400',
] as const;
