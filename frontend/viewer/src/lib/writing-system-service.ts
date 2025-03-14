﻿import type {IEntry, IExampleSentence, IMultiString, ISense, IWritingSystem, IWritingSystems} from '$lib/dotnet-types';
import {getContext, onDestroy, setContext} from 'svelte';
import {derived, get, type Readable} from 'svelte/store';
import type {WritingSystemSelection} from './config-types';
import {firstTruthy} from './utils';

const writingSystemContextName = 'writingSystems';
export function initWritingSystemService(ws: Readable<IWritingSystems | null>): Readable<WritingSystemService | null> {
  return setContext(writingSystemContextName, derived(ws, ($ws) => $ws ? new WritingSystemService($ws) : null));
}

export function useWritingSystemService(): WritingSystemService {
  const writingSystemServiceContext = getContext<Readable<WritingSystemService | null>>(writingSystemContextName);
  if (!writingSystemServiceContext) throw new Error('Writing system context is not initialized. Are you in the context of a project?');
  const writingSystemService = get(writingSystemServiceContext);
  if (!writingSystemService) throw new Error('Writing system service is not initialized.');
  const unsub = writingSystemServiceContext.subscribe((service) => {
    if (service !== writingSystemService) console.warn('Writing system service changed unexpectedly.');
  });
  onDestroy(unsub);
  return writingSystemService;
}

export class WritingSystemService {

  private readonly wsColors: WritingSystemColors;

  constructor(private readonly writingSystems: IWritingSystems) {
    this.wsColors = calcWritingSystemColors(writingSystems);
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

  defaultVernacular(): IWritingSystem | undefined {
    return this.writingSystems.vernacular[0];
  }

  defaultAnalysis(): IWritingSystem | undefined {
    return this.writingSystems.analysis[0];
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
    }
    console.error(`Unknown writing system selection: ${ws as string}`);
    return [];
  }

  indexExemplars(): string[] | undefined {
    return this.defaultVernacular()?.exemplars;
  }

  headword(entry: IEntry, ws?: string): string {
    if (ws) {
      return WritingSystemService.headword(entry, ws) || '';
    }

    return firstTruthy(this.vernacular, ws => WritingSystemService.headword(entry, ws.wsId)) || '';
  }

  private static headword(entry: IEntry, ws: string): string | undefined {
    return entry.citationForm[ws] || entry.lexemeForm[ws];
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
    return this.first(example.sentence, this.vernacular) || this.first(example.translation, this.analysis) || '';
  }

  wsColor(ws: string, type: 'vernacular' | 'analysis'): string {
    const colors = this.wsColors[type];
    return colors[ws];
  }

  private first(value: IMultiString, writingSystems: IWritingSystem[]): string | undefined {
    return firstTruthy(writingSystems, ws => value[ws.wsId]);
  }
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
  'text-emerald-400 dark:text-emerald-300',
  'text-fuchsia-600 dark:text-fuchsia-300',
  'text-lime-600 dark:text-lime-200',
] as const;

const analysisColors = [
  'text-blue-500 dark:text-blue-300',
  'text-yellow-500 dark:text-yellow-200',
  'text-rose-500 dark:text-rose-400',
] as const;
