<script lang="ts" context="module">
  // Define regex here so browsers can compile it *once* and reuse it
  const logEntryRe = /\[[^\]]+\]/;
</script>

<script lang="ts">
  import t from '$lib/i18n';
  import type { Readable } from 'svelte/store';

  import FormatDate from './FormatDate.svelte';
  import type { Circle, Path } from './TrainTracks.svelte';
  import TrainTracks from './TrainTracks.svelte';
  import Loader from './Loader.svelte';
  import type { ProjectChangesetsQuery } from '$lib/gql/types';

  type LogEntries = NonNullable<ProjectChangesetsQuery['projectByCode']>['changesets'];
  type LogEntry = NonNullable<LogEntries>[0];
  type ExpandedLogEntry = LogEntry & {
    trimmedLog: string;
    row: number;
    col: number;
  };

  export let logEntryStore: Readable<{changesets: LogEntries | undefined, loading: boolean}>;
  $: logEntries = $logEntryStore.changesets; // JSON-format hg log

  function assignRowsAndColumns(entries: ExpandedLogEntry[]): void {
    // Walk the log top-down (most recent entry first) and assign circle locations for each log entry ("node")
    // Basic idea is to keep them as far left as possible, only assigning extra columns when a node had multiple parents (a merge)
    // Nodes with multiple children usually represent forks in the original repo, and those are placed under their leftmost child
    const cols = {} as { [node: string]: number };
    let curCol = 0;
    let curRow = 0;

    for (const entry of entries) {
      entry.row = curRow;
      curRow++;
      const { node, parents } = entry;
      if (node in cols) {
        // Already seen
        curCol = cols[node];
      } else {
        // Not seen yet, so assign current column
        cols[node] = curCol;
      }
      entry.col = curCol;
      const parentCount = parents.length;
      if (parentCount == 0) {
        // Nothing to do. Will only happen on the last row.
      } else if (parentCount == 1) {
        const parent = parents[0];
        if (parent in cols) {
          // Parent has already been assigned a column, but maybe it can move to the left.
          // Therefore, use either its column or the current column, *whichever is lower* (important)
          // This typically happens when a node had multiple children, i.e. it was a fork point, and by
          // moving as far left as possible we usually end up under the leftmost child, often in column 0
          curCol = Math.min(cols[parent], curCol);
          cols[parent] = curCol;
        } else {
          cols[parent] = curCol;
        }
      } else {
        let colsAssigned = 0;
        for (const parent of parents) {
          if (parent in cols) {
            // Don't touch already-assigned column
          } else {
            // First new (not seen yet) parent gets current column
            // Subsequent ones get one more column to the right
            if (colsAssigned == 0) {
              cols[parent] = curCol;
            } else {
              curCol++;
              cols[parent] = curCol;
            }
            colsAssigned++;
          }
        }
      }
    }
  }

  function assignPaths(entries: ExpandedLogEntry[]): Path[] {
    let indices = {} as { [node: string]: number };
    let paths: Path[] = [];
    for (const entry of entries) {
      const { node, row } = entry;
      indices[node] = row;
    }
    let curIdx = 0;
    for (const entry of entries) {
      const { parents } = entry;
      for (const parent of parents) {
        const parentIdx = indices[parent];
        paths.push({ fromIdx: curIdx, toIdx: parentIdx });
      }
      curIdx++;
    }
    return paths;
  }

  let expandedLog: ExpandedLogEntry[];
  $: {
    expandedLog = (logEntries ?? []) as ExpandedLogEntry[];
    assignRowsAndColumns(expandedLog);
    expandedLog = expandedLog.map((e) => ({
      ...e,
      trimmedLog: trimEntry(e.desc),
    }));
  }

  function trimEntry(orig: string): string {
    // The [program: version string] part of log entries can get quite long, so let's trim it for the graph
    return orig.replace(logEntryRe, '');
  }

  $: circles = expandedLog.map((entry, idx): Circle => ({ row: idx, col: entry.col }));
  $: paths = assignPaths(expandedLog);

  let heights: number[] = [];
</script>

<table class="table table-zebra">
  <thead>
    <tr class="sticky top-0 z-[1] bg-base-100">
      <th></th>
      <th>{$t('project_page.hg.date_header')}</th>
      <th>{$t('project_page.hg.author_header')}</th>
      <th>{$t('project_page.hg.log_header')}</th>
    </tr>
  </thead>
  <tbody>
    {#if logEntries?.length}
      {#each expandedLog as log, idx}
        <tr>
          {#if idx === 0}
            <td class="py-0 w-0" rowspan="1000000">
              <TrainTracks {circles} {paths} rowHeights={heights} />
            </td>
          {/if}
          <td bind:offsetHeight={heights[idx]}><FormatDate date={log.date[0] * 1000} /></td>
          <td>{log.user}</td>
          <td>{log.trimmedLog}</td>
        </tr>
      {/each}
    {:else}
      <tr>
        <td colspan="100">
          <div class="text p-2 text-secondary flex gap-2 items-center">
            {#if $logEntryStore.loading}
              <Loader loading />
              {$t('project_page.hg.loading')}
            {:else}
              <span class="i-mdi-creation-outline text-2xl" />
              {$t('project_page.hg.no_history')}
            {/if}
          </div>
        </td>
      </tr>
    {/if}
  </tbody>
</table>

<style lang="postcss">
  /* These make "height: 100%" work on the train tracks SVG */
  table {
    @apply h-1;

    & tr, td {
      height: 100%;
    }
  }
</style>
