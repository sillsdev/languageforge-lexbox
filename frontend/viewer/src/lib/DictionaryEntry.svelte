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
  import { headword, pickBestAlternative } from './utils';
  import {useWritingSystems} from './writing-systems';
  import { usePartsOfSpeech } from './parts-of-speech';

  export let entry: IEntry;
  export let lines: number = 0;

  $: lines = entry.senses.length > 1 ? entry.senses.length + 1 : 1;

  const allWritingSystems = useWritingSystems();
  const wsColor = derived(allWritingSystems, ({vernacular, analysis}) => {
    const vernacularColor: Record<string, typeof vernacularColors[number]> = {};
    const analysisColor: Record<string, typeof analysisColors[number]> = {};
    vernacular.forEach((ws, i) => {
      vernacularColor[ws.wsId] = vernacularColors[i % vernacularColors.length];
    });
    analysis.forEach((ws, i) => {
      analysisColor[ws.wsId] = analysisColors[i % analysisColors.length];
    });
    return (ws: string, type: 'vernacular' | 'analysis') => {
      const colors = type === 'vernacular' ? vernacularColor : analysisColor;
      return colors[ws];
    };
  });

  $: headwords = $allWritingSystems.vernacular
    .map(ws => ({ws: ws.wsId, value: headword(entry, ws.wsId)}))
    .filter(({value}) => !!value);

  const partsOfSpeech = usePartsOfSpeech();
  function partOfSpeechLabel(id: string | undefined): string | undefined {
    if (!id) return undefined;
    const partOfSpeech = $partsOfSpeech.find(pos => pos.id === id);
    if (!partOfSpeech) return undefined;
    return pickBestAlternative(partOfSpeech.name, 'analysis', $allWritingSystems)
  }
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
    {@const partOfSpeech = partOfSpeechLabel(sense.partOfSpeechId)}
    {#if partOfSpeech}
      <i>{partOfSpeech}.</i>
    {/if}
    <span>
      {#each $allWritingSystems.analysis as ws}
        {#if sense.gloss[ws.wsId] || sense.definition[ws.wsId]}
          <span class="ml-0.5">
            <sub class="-mr-0.5">{ws.abbreviation}</sub>
            {#if sense.gloss[ws.wsId]}
              <span class={$wsColor(ws.wsId, 'analysis')}>{sense.gloss[ws.wsId]}</span>;
            {/if}
            {#if sense.definition[ws.wsId]}
              <span class={$wsColor(ws.wsId, 'analysis')}>{sense.definition[ws.wsId]}</span>
            {/if}
          </span>
        {/if}
      {/each}
    </span>
    {#each sense.exampleSentences as example (example.id)}
      {@const usedVernacular = $allWritingSystems.vernacular.filter(ws => !!example.sentence[ws.wsId])}
      {@const usedAnalysis = $allWritingSystems.analysis.filter(ws => !!example.translation[ws.wsId])}
      {#if usedVernacular.length || usedAnalysis.length}
        <span class="-mr-0.5">[</span>
          {#each usedVernacular as ws}
            <span class={$wsColor(ws.wsId, 'vernacular')}>{example.sentence[ws.wsId]}</span>
            <span></span><!-- standardizes whitespace between texts -->
          {/each}
          <span></span>
          {#each usedAnalysis as ws}
            <span class={$wsColor(ws.wsId, 'analysis')}>{example.translation[ws.wsId]}</span>
            <span></span><!-- standardizes whitespace between texts -->
          {/each}
        <span class="-ml-0.5">]</span>
        <span></span><!-- standardizes whitespace between texts -->
      {/if}
    {/each}
  {/each}
</div>
