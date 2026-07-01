import {useDebounce} from 'runed';
import type {IMiniLcmJsInvokable, IWritingSystem} from '$lib/dotnet-types';
import {useProjectEventBus} from '$lib/services/event-bus';
import {useProjectContext} from '$project/project-context.svelte';
import {useWritingSystemService} from '$project/data';

export const ENTRY_GOAL = 10_000;

export type FieldCompletion = {
  filled: number;
  percent: number;
};

export type VernacularWritingSystemStats = {
  wsId: string;
  abbreviation: string;
  lexemeForm: FieldCompletion;
  citationForm: FieldCompletion;
  exampleSentence: FieldCompletion;
};

export type AnalysisWritingSystemStats = {
  wsId: string;
  abbreviation: string;
  gloss: FieldCompletion;
  definition: FieldCompletion;
};

export type DashboardStats = {
  totalEntries: number;
  entriesWithSenses: number;
  entriesWithExamples: number;
  goalTarget: number;
  goalPercent: number;
  goalRemaining: number;
  vernacular: VernacularWritingSystemStats[];
  analysis: AnalysisWritingSystemStats[];
};

const dashboardStatsSymbol = Symbol.for('fw-lite-dashboard-stats');

async function countMissing(api: IMiniLcmJsInvokable, gridifyFilter: string): Promise<number> {
  return api.countEntries(undefined, {filter: {gridifyFilter}});
}

function completion(total: number, filled: number): FieldCompletion {
  return {
    filled,
    percent: total === 0 ? 0 : Math.round((filled / total) * 100),
  };
}

async function filledCount(api: IMiniLcmJsInvokable, total: number, gridifyFilter: string): Promise<number> {
  const missing = await countMissing(api, gridifyFilter);
  return total - missing;
}

function mapVernacularStats(
  totalEntries: number,
  writingSystems: IWritingSystem[],
  filledCounts: number[],
): VernacularWritingSystemStats[] {
  let index = 0;
  return writingSystems.map(ws => ({
    wsId: ws.wsId,
    abbreviation: ws.abbreviation,
    lexemeForm: completion(totalEntries, filledCounts[index++]),
    citationForm: completion(totalEntries, filledCounts[index++]),
    exampleSentence: completion(totalEntries, filledCounts[index++]),
  }));
}

function mapAnalysisStats(
  totalEntries: number,
  writingSystems: IWritingSystem[],
  filledCounts: number[],
): AnalysisWritingSystemStats[] {
  let index = 0;
  return writingSystems.map(ws => ({
    wsId: ws.wsId,
    abbreviation: ws.abbreviation,
    gloss: completion(totalEntries, filledCounts[index++]),
    definition: completion(totalEntries, filledCounts[index++]),
  }));
}

async function fetchDashboardStats(
  api: IMiniLcmJsInvokable,
  vernacular: IWritingSystem[],
  analysis: IWritingSystem[],
): Promise<DashboardStats> {
  const totalEntries = await api.countEntries(undefined, undefined);

  const [entriesWithSenses, entriesWithExamples, vernacularFilled, analysisFilled] = await Promise.all([
    filledCount(api, totalEntries, 'Senses=null'),
    filledCount(api, totalEntries, 'Senses.ExampleSentences=null'),
    Promise.all(vernacular.flatMap(ws => [
      filledCount(api, totalEntries, `LexemeForm[${ws.wsId}]=`),
      filledCount(api, totalEntries, `CitationForm[${ws.wsId}]=`),
      filledCount(api, totalEntries, `Senses.ExampleSentences=null|Senses.ExampleSentences.Sentence[${ws.wsId}]=`),
    ])),
    Promise.all(analysis.flatMap(ws => [
      filledCount(api, totalEntries, `Senses=null|Senses.Gloss[${ws.wsId}]=`),
      filledCount(api, totalEntries, `Senses=null|Senses.Definition[${ws.wsId}]=`),
    ])),
  ]);

  return {
    totalEntries,
    entriesWithSenses,
    entriesWithExamples,
    goalTarget: ENTRY_GOAL,
    goalPercent: totalEntries === 0 ? 0 : Math.min(100, Math.round((totalEntries / ENTRY_GOAL) * 100)),
    goalRemaining: Math.max(0, ENTRY_GOAL - totalEntries),
    vernacular: mapVernacularStats(totalEntries, vernacular, vernacularFilled),
    analysis: mapAnalysisStats(totalEntries, analysis, analysisFilled),
  };
}

export function useDashboardStats() {
  const projectContext = useProjectContext();
  const projectEventBus = useProjectEventBus();
  const writingSystemService = useWritingSystemService();

  const statsResource = projectContext.getOrAddAsync<DashboardStats | undefined>(
    dashboardStatsSymbol,
    undefined,
    async (api) => {
      let vernacular = writingSystemService.vernacular;
      let analysis = writingSystemService.analysis;
      if (vernacular.length === 0 && analysis.length === 0) {
        const writingSystems = await api.getWritingSystems();
        vernacular = writingSystems.vernacular;
        analysis = writingSystems.analysis;
      }
      return fetchDashboardStats(api, vernacular, analysis);
    },
    {
      onAdd: (resource) => {
        const debouncedRefetch = useDebounce(() => void resource.refetch(), 500);
        projectEventBus.onEntryDeleted(() => void debouncedRefetch());
        projectEventBus.onEntryUpdated(() => void debouncedRefetch());
      },
    },
  );

  return statsResource;
}
