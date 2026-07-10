import {useDebounce} from 'runed';
import type {IMiniLcmJsInvokable, IWritingSystem} from '$lib/dotnet-types';
import {useProjectEventBus} from '$lib/services/event-bus';
import {useProjectContext} from '$project/project-context.svelte';
import {useWritingSystemService, type WritingSystemService} from '$project/data';

export const MILESTONES = [50, 100, 250, 500, 1_000, 2_500, 5_000, 10_000, 25_000, 50_000, 100_000];

export type FieldCompletion = {
  filled: number;
  percent: number;
};

export type MilestoneProgress = {
  target: number;
  remaining: number;
  percent: number;
};

function nextMilestone(totalEntries: number): MilestoneProgress | undefined {
  const target = MILESTONES.find(m => m > totalEntries);
  if (target === undefined) return undefined;
  return {
    target,
    remaining: target - totalEntries,
    percent: Math.min(100, Math.round((totalEntries / target) * 100)),
  };
}

export type VernacularWritingSystemStats = {
  wsId: string;
  abbreviation: string;
  lexemeForm?: FieldCompletion;
  citationForm?: FieldCompletion;
  exampleSentence?: FieldCompletion;
};

export type AnalysisWritingSystemStats = {
  wsId: string;
  abbreviation: string;
  gloss?: FieldCompletion;
  definition?: FieldCompletion;
};

const dashboardStatsSymbol = Symbol.for('fw-lite-dashboard-stats');

function completion(total: number, filled: number): FieldCompletion {
  return {
    filled,
    percent: total === 0 ? 0 : Math.round((filled / total) * 100),
  };
}

function seedRows<T extends {wsId: string}>(
  writingSystems: IWritingSystem[],
  previous: T[] | undefined,
  make: (ws: IWritingSystem) => T,
): T[] {
  const prev = new Map(previous?.map(row => [row.wsId, row]));
  return writingSystems.map(ws => prev.get(ws.wsId) ?? make(ws));
}

/**
 * Fills in progressively: every stat is undefined until its count arrives, so the view
 * can render immediately with placeholders. On reload, stats keep their previous values
 * until the new counts land.
 */
export class DashboardStats {
  totalEntries = $state<number>();
  entriesWithSenses = $state<FieldCompletion>();
  entriesWithExamples = $state<FieldCompletion>();
  /** undefined until the writing system list is known; [] means none configured */
  vernacular = $state<VernacularWritingSystemStats[]>();
  analysis = $state<AnalysisWritingSystemStats[]>();
  readonly milestone: MilestoneProgress | undefined = $derived(
    this.totalEntries === undefined ? undefined : nextMilestone(this.totalEntries));

  #writingSystemService: WritingSystemService;
  #loadVersion = 0;

  constructor(writingSystemService: WritingSystemService) {
    this.#writingSystemService = writingSystemService;
  }

  #patchVernacularRow(
    wsId: string,
    patch: Partial<Pick<VernacularWritingSystemStats, 'lexemeForm' | 'citationForm' | 'exampleSentence'>>,
  ): void {
    const row = this.vernacular?.find(r => r.wsId === wsId);
    if (row) Object.assign(row, patch);
  }

  #patchAnalysisRow(
    wsId: string,
    patch: Partial<Pick<AnalysisWritingSystemStats, 'gloss' | 'definition'>>,
  ): void {
    const row = this.analysis?.find(r => r.wsId === wsId);
    if (row) Object.assign(row, patch);
  }

  async load(api: IMiniLcmJsInvokable): Promise<void> {
    const version = ++this.#loadVersion;
    const fresh = () => version === this.#loadVersion;

    const totalEntries = await api.countEntries(undefined, undefined);
    if (!fresh()) return;
    this.totalEntries = totalEntries;

    async function filledField(gridifyFilter: string, assign: (field: FieldCompletion) => void): Promise<void> {
      const missing = await api.countEntries(undefined, {filter: {gridifyFilter}});
      if (!fresh()) return;
      assign(completion(totalEntries, totalEntries - missing));
    }

    const primary = Promise.all([
      filledField('Senses=null', f => { this.entriesWithSenses = f; }),
      filledField('Senses.ExampleSentences=null', f => { this.entriesWithExamples = f; }),
    ]);

    let vernacular = this.#writingSystemService.vernacular;
    let analysis = this.#writingSystemService.analysis;
    if (vernacular.length === 0 && analysis.length === 0) {
      const writingSystems = await api.getWritingSystems();
      if (!fresh()) return;
      vernacular = writingSystems.vernacular;
      analysis = writingSystems.analysis;
    }

    this.vernacular = seedRows(vernacular, this.vernacular, ws => ({wsId: ws.wsId, abbreviation: ws.abbreviation}));
    this.analysis = seedRows(analysis, this.analysis, ws => ({wsId: ws.wsId, abbreviation: ws.abbreviation}));
    // read back so the rows are $state proxies and the field updates below are reactive
    const vernacularRows = this.vernacular;
    const analysisRows = this.analysis;

    // deliberately not one big Promise.all: the per-writing-system counts would compete with the primary stats
    await primary;
    if (!fresh()) return;

    await Promise.all([
      ...vernacularRows.flatMap(row => [
        filledField(`LexemeForm[${row.wsId}]=`, f => this.#patchVernacularRow(row.wsId, {lexemeForm: f})),
        filledField(`CitationForm[${row.wsId}]=`, f => this.#patchVernacularRow(row.wsId, {citationForm: f})),
        filledField(
          `Senses.ExampleSentences=null|Senses.ExampleSentences.Sentence[${row.wsId}]=`,
          f => this.#patchVernacularRow(row.wsId, {exampleSentence: f}),
        ),
      ]),
      ...analysisRows.flatMap(row => [
        filledField(`Senses=null|Senses.Gloss[${row.wsId}]=`, f => this.#patchAnalysisRow(row.wsId, {gloss: f})),
        filledField(`Senses=null|Senses.Definition[${row.wsId}]=`, f => this.#patchAnalysisRow(row.wsId, {definition: f})),
      ]),
    ]);
  }
}

export function useDashboardStats() {
  const projectContext = useProjectContext();
  const projectEventBus = useProjectEventBus();
  const writingSystemService = useWritingSystemService();

  return projectContext.getOrAdd(dashboardStatsSymbol, () => {
    const stats = new DashboardStats(writingSystemService);
    const resource = projectContext.apiResource(stats, async (api) => {
      await stats.load(api);
      return stats;
    });
    const debouncedRefetch = useDebounce(() => void resource.refetch(), 500);
    projectEventBus.onEntryDeleted(() => void debouncedRefetch());
    projectEventBus.onEntryUpdated(() => void debouncedRefetch());
    return resource;
  });
}
