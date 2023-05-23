<script lang="ts" context="module">
    // Define regex here so browsers can compile it *once* and reuse it
    const logEntryRe = /\[[^\]]+\]/;
</script>

<script lang="ts">
    import TrainTracks from './TrainTracks.svelte';

    type LogEntry = {
        node: string,
        parents: string[],
        date: [number, number],
        user: string,
        desc: string
    };
    type ExpandedLogEntry = LogEntry & {
        trimmedLog: string,
        row: number,
        col: number
    };

    export let json: LogEntry[];  // JSON-format hg log

    function assignRowsAndColumns(entries: ExpandedLogEntry[]) {
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

    function assignPaths(entries: ExpandedLogEntry[]) {
        let indices = {} as { [node: string]: number };
        let paths: [number, number][] = [];
        for (const entry of entries) {
            const { node, row } = entry;
            indices[node] = row;
        }
        let curIdx = 0;
        for (const entry of entries) {
            const { parents } = entry;
            for (const parent of parents) {
                const parentIdx = indices[parent];
                paths.push([curIdx, parentIdx]);
            }
            curIdx++;
        }
        return paths;
    }

    let expandedLog:  ExpandedLogEntry[];
    $: {
        expandedLog = json as ExpandedLogEntry[];
        assignRowsAndColumns(expandedLog);
        expandedLog = expandedLog.map(e => ({...e, trimmedLog: trimEntry(e.desc)}))
    };

    function trimEntry(orig: string) {
        // The [program: version string] part of log entries can get quite long, so let's trim it for the graph
        return orig.replace(logEntryRe, "");
    }

    $: logs = json.map

    $: circles = expandedLog.map((entry, idx) => ({ row: idx, col: entry.col }));
    $: paths = assignPaths(expandedLog);

    let heights: number[] = [];
</script>

<!-- TODO: Create table with log entries, then capture row heights -->
<div class="horiz">
<TrainTracks {circles} {paths} rowHeights={heights} />
<table>
    {#each expandedLog as log, idx}
        <tr bind:clientHeight={heights[idx]}><td>{log.desc}</td></tr>
    {/each}
</table>
</div>

<style>
    .horiz {
        flex: 1;
        display: flex;
        flex-direction: row;
    }
</style>