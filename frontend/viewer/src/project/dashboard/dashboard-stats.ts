import {useDebounce} from 'runed';
import type {IMiniLcmJsInvokable, IWritingSystem} from '$lib/dotnet-types';
import {useProjectEventBus} from '$lib/services/event-bus';
import {useProjectContext} from '$project/project-context.svelte';
import {useWritingSystemService} from '$project/data';

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
  entriesWithSenses: FieldCompletion;
  entriesWithExamples: FieldCompletion;
  milestone: MilestoneProgress | undefined;
  vernacular: VernacularWritingSystemStats[];
  analysis: AnalysisWritingSystemStats[];
};

type VernacularFilledCounts = {
  lexemeForm: number;
  citationForm: number;
  exampleSentence: number;
};

type AnalysisFilledCounts = {
  gloss: number;
  definition: number;
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
  filledCounts: VernacularFilledCounts[],
): VernacularWritingSystemStats[] {
  return writingSystems.map((ws, i) => {
    const counts = filledCounts[i];
    return {
      wsId: ws.wsId,
      abbreviation: ws.abbreviation,
      lexemeForm: completion(totalEntries, counts.lexemeForm),
      citationForm: completion(totalEntries, counts.citationForm),
      exampleSentence: completion(totalEntries, counts.exampleSentence),
    };
  });
}

function mapAnalysisStats(
  totalEntries: number,
  writingSystems: IWritingSystem[],
  filledCounts: AnalysisFilledCounts[],
): AnalysisWritingSystemStats[] {
  return writingSystems.map((ws, i) => {
    const counts = filledCounts[i];
    return {
      wsId: ws.wsId,
      abbreviation: ws.abbreviation,
      gloss: completion(totalEntries, counts.gloss),
      definition: completion(totalEntries, counts.definition),
    };
  });
}

async function fetchDashboardStats(
  api: IMiniLcmJsInvokable,
  vernacular: IWritingSystem[],
  analysis: IWritingSystem[],
): Promise<DashboardStats> {
  const totalEntries = await api.countEntries(undefined, undefined);

  const [entriesWithSensesCount, entriesWithExamplesCount, vernacularFilled, analysisFilled] = await Promise.all([
    filledCount(api, totalEntries, 'Senses=null'),
    filledCount(api, totalEntries, 'Senses.ExampleSentences=null'),
    Promise.all(vernacular.map(async ws => ({
      lexemeForm: await filledCount(api, totalEntries, `LexemeForm[${ws.wsId}]=`),
      citationForm: await filledCount(api, totalEntries, `CitationForm[${ws.wsId}]=`),
      exampleSentence: await filledCount(api, totalEntries, `Senses.ExampleSentences=null|Senses.ExampleSentences.Sentence[${ws.wsId}]=`),
    }))),
    Promise.all(analysis.map(async ws => ({
      gloss: await filledCount(api, totalEntries, `Senses=null|Senses.Gloss[${ws.wsId}]=`),
      definition: await filledCount(api, totalEntries, `Senses=null|Senses.Definition[${ws.wsId}]=`),
    }))),
  ]);

  return {
    totalEntries,
    entriesWithSenses: completion(totalEntries, entriesWithSensesCount),
    entriesWithExamples: completion(totalEntries, entriesWithExamplesCount),
    milestone: nextMilestone(totalEntries),
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
