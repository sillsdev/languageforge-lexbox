import {useDebounce} from 'runed';
import type {IMiniLcmJsInvokable, IWritingSystem} from '$lib/dotnet-types';
import {useProjectEventBus} from '$lib/services/event-bus';
import {useProjectContext} from '$project/project-context.svelte';
import {useWritingSystemService, type WritingSystemService} from '$project/data';
import {TasksService} from '$project/tasks/tasks-service';
import type {FieldId} from '$lib/views/entity-config';

export const MILESTONES = [50, 100, 250, 500, 1_000, 2_500, 5_000, 10_000, 25_000, 50_000, 100_000];

export type FieldCompletion = {
  filled: number;
  total: number;
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
  translation?: FieldCompletion;
};

const dashboardStatsSymbol = Symbol.for('fw-lite-dashboard-stats');

function completion(total: number, filled: number): FieldCompletion {
  return {
    filled,
    total,
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
  entriesWithPartOfSpeech = $state<FieldCompletion>();
  entriesWithSemanticDomains = $state<FieldCompletion>();
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
    patch: Partial<Pick<AnalysisWritingSystemStats, 'gloss' | 'definition' | 'translation'>>,
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

    // missingOutside: entries the filter matches that aren't part of total, e.g. entries without senses
    async function filledField(
      gridifyFilter: string,
      assign: (field: FieldCompletion) => void,
      total = totalEntries,
      missingOutside = 0,
    ): Promise<void> {
      const missing = await api.countEntries(undefined, {filter: {gridifyFilter}});
      if (!fresh()) return;
      assign(completion(total, Math.max(0, total - (missing - missingOutside))));
    }

    const primary = Promise.all([
      api.countEntries(undefined, {filter: {gridifyFilter: 'Senses=null'}}).then(missing => totalEntries - missing),
      api.countEntries(undefined, {filter: {gridifyFilter: 'Senses.ExampleSentences=null'}}).then(missing => totalEntries - missing),
    ]).then(([withSenses, withExamples]) => {
      if (fresh()) {
        this.entriesWithSenses = completion(totalEntries, withSenses);
        this.entriesWithExamples = completion(totalEntries, withExamples);
      }
      return [withSenses, withExamples] as const;
    });

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
    const [withSenses, withExamples] = await primary;
    if (!fresh()) return;

    // The rows link to these tasks, so they must count with the same filters.
    const tasks = [
      ...TasksService.makeEntryTasks(vernacular),
      ...TasksService.makeSenseTasks(analysis),
      ...TasksService.makeExampleSentenceTasks(vernacular),
      ...TasksService.makeTranslationTasks(analysis, vernacular),
    ];
    function taskFilter(field: FieldId, wsId?: string): string {
      const filter = TasksService.findIn(tasks, field, wsId)?.gridifyFilter;
      if (!filter) throw new Error(`No task counts ${field} in ${wsId}`);
      return filter;
    }

    // Count part of speech, domains and translations against the entries that can have them.
    // The part of speech filter skips entries without senses; the semantic domain filter matches them.
    await Promise.all([
      filledField(taskFilter('partOfSpeechId'), f => { this.entriesWithPartOfSpeech = f; }, withSenses),
      filledField(taskFilter('semanticDomains'), f => { this.entriesWithSemanticDomains = f; }, withSenses, totalEntries - withSenses),
      ...vernacularRows.flatMap(row => [
        filledField(taskFilter('lexemeForm', row.wsId), f => this.#patchVernacularRow(row.wsId, {lexemeForm: f})),
        filledField(taskFilter('citationForm', row.wsId), f => this.#patchVernacularRow(row.wsId, {citationForm: f})),
        filledField(taskFilter('sentence', row.wsId), f => this.#patchVernacularRow(row.wsId, {exampleSentence: f})),
      ]),
      ...analysisRows.flatMap(row => [
        filledField(taskFilter('gloss', row.wsId), f => this.#patchAnalysisRow(row.wsId, {gloss: f})),
        filledField(taskFilter('definition', row.wsId), f => this.#patchAnalysisRow(row.wsId, {definition: f})),
        filledField(taskFilter('translations', row.wsId), f => this.#patchAnalysisRow(row.wsId, {translation: f}), withExamples),
      ]),
    ]);
  }
}

export function useDashboardStats() {
  const projectContext = useProjectContext();
  const projectEventBus = useProjectEventBus();
  const writingSystemService = useWritingSystemService();

  const resource = projectContext.getOrAdd(dashboardStatsSymbol, () => {
    const stats = new DashboardStats(writingSystemService);
    return projectContext.apiResource(stats, async (api) => {
      await stats.load(api);
      return stats;
    });
  });
  // Outside the cached factory, so the subscription ends with the dashboard. Catch up on what changed meanwhile.
  const debouncedRefetch = useDebounce(() => void resource.refetch(), 500);
  projectEventBus.onEntriesChanged(() => void debouncedRefetch());
  void resource.refetch();
  return resource;
}
