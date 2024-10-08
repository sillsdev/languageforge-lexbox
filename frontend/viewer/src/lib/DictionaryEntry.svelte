<script lang="ts" context="module">
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
</script>

<script lang="ts">
  import { derived } from 'svelte/store';

  import type { IEntry } from './mini-lcm';
  import { headword } from './utils';
  import {useWritingSystems} from './writing-systems';

  export let entry: IEntry;
  export let lines: number = 0;

  $: lines = entry.senses.length > 1 ? entry.senses.length + 1 : 1;

  const allWritingSystems = useWritingSystems();
  const wsColor = derived(allWritingSystems, ({vernacular, analysis}) => {
    const vernacularColor: Record<string, typeof vernacularColors[number]> = {};
    const analysisColor: Record<string, typeof analysisColors[number]> = {};
    vernacular.forEach((ws, i) => {
      vernacularColor[ws.id] = vernacularColors[i % vernacularColors.length];
    });
    analysis.forEach((ws, i) => {
      analysisColor[ws.id] = analysisColors[i % analysisColors.length];
    });
    return (ws: string, type: 'vernacular' | 'analysis') => {
      const colors = type === 'vernacular' ? vernacularColor : analysisColor;
      return colors[ws];
    };
  });

  $: headwords = $allWritingSystems.vernacular
    .map(ws => ({ws: ws.id, value: headword(entry, ws.id)}))
    .filter(({value}) => !!value);
</script>

<div>
  <strong class="inline-flex gap-1">
    {#each headwords as {ws, value}, i}
      {#if value}
        {#if i > 0}/{/if}
        <span class={$wsColor(ws, 'vernacular')}>{value}</span>
      {/if}
    {/each}
  </strong>
  {#each entry.senses as sense, i (sense.id)}
    {#if entry.senses.length > 1}
      <br />
      <strong class="ml-2">{i + 1} · </strong>
    {/if}
    {#if sense.partOfSpeech}
      <i>{sense.partOfSpeech}.</i>
    {/if}
    <span>
      {#each $allWritingSystems.analysis as ws}
        {#if sense.gloss[ws.id] || sense.definition[ws.id]}
          <span class="ml-0.5">
            <sub class="-mr-0.5">{ws.abbreviation}</sub>
            {#if sense.gloss[ws.id]}
              <span class={$wsColor(ws.id, 'analysis')}>{sense.gloss[ws.id]}</span>;
            {/if}
            {#if sense.definition[ws.id]}
              <span class={$wsColor(ws.id, 'analysis')}>{sense.definition[ws.id]}</span>
            {/if}
          </span>
        {/if}
      {/each}
    </span>
    {#each sense.exampleSentences as example, i (example.id)}
      {@const usedVernacular = $allWritingSystems.vernacular.filter(ws => !!example.sentence[ws.id])}
      {@const usedAnalysis = $allWritingSystems.analysis.filter(ws => !!example.translation[ws.id])}
      {#if usedVernacular.length || usedAnalysis.length}
        <span class="-mr-0.5">[</span>
          {#each usedVernacular as ws}
            <span class={$wsColor(ws.id, 'vernacular')}>{example.sentence[ws.id]}</span>
            <span></span><!-- standardizes whitespace between texts -->
          {/each}
          <span></span>
          {#each usedAnalysis as ws}
            <span class={$wsColor(ws.id, 'analysis')}>{example.translation[ws.id]}</span>
            <span></span><!-- standardizes whitespace between texts -->
          {/each}
        <span class="-ml-0.5">]</span>
        <span></span><!-- standardizes whitespace between texts -->
      {/if}
    {/each}
  {/each}
</div>
