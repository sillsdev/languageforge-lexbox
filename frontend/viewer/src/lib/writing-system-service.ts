import type {IEntry, IExampleSentence, IMultiString, ISense, IWritingSystem, IWritingSystems} from '$lib/dotnet-types';
import {getContext, setContext} from 'svelte';
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
  return writingSystemService;
}

export class WritingSystemService {

  constructor(private readonly writingSystems: IWritingSystems) { }

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
        return [...new Set([...this.writingSystems.vernacular, ...this.writingSystems.analysis].sort())];
      case 'analysis-vernacular':
        return [...new Set([...this.writingSystems.analysis, ...this.writingSystems.vernacular].sort())];
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

  private first(value: IMultiString, writingSystems: IWritingSystem[]): string | undefined {
    return firstTruthy(writingSystems, ws => value[ws.wsId]);
  }
}
