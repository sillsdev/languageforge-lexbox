<script lang="ts">
  import type { IEntry } from '$lib/dotnet-types';
  import {useWritingSystemService} from './writing-system-service';
  import { usePartsOfSpeech } from './parts-of-speech';

  export let entry: IEntry;
  export let lines: number = 0;

  $: lines = entry.senses.length > 1 ? entry.senses.length + 1 : 1;

  const wsService = useWritingSystemService();

  $: headwords = wsService.vernacular
    .map(ws => ({ws: ws.wsId, value: wsService.headword(entry)}))
    .filter(({value}) => !!value);

  const partsOfSpeech = usePartsOfSpeech();
</script>

<div>
  <strong class="inline-flex gap-1">
    {#each headwords as {ws, value}, i}
      {#if value}
        {#if i > 0}/{/if}
        <span class={wsService.wsColor(ws, 'vernacular')}>{value}</span>
      {/if}
    {/each}
  </strong>
  {#each entry.senses as sense, i (sense.id)}
    {#if entry.senses.length > 1}
      <br />
      <strong class="ml-2">{i + 1} · </strong>
    {/if}
    {@const partOfSpeech = $partsOfSpeech.find(pos => pos.id === sense.partOfSpeechId)?.label}
    {#if partOfSpeech}
      <i>{partOfSpeech}</i>
    {/if}
    <span>
      {#each wsService.analysis as ws}
        {#if sense.gloss[ws.wsId] || sense.definition[ws.wsId]}
          <span class="ml-0.5">
            <sub class="-mr-0.5">{ws.abbreviation}</sub>
            {#if sense.gloss[ws.wsId]}
              <span class={wsService.wsColor(ws.wsId, 'analysis')}>{sense.gloss[ws.wsId]}</span>;
            {/if}
            {#if sense.definition[ws.wsId]}
              <span class={wsService.wsColor(ws.wsId, 'analysis')}>{sense.definition[ws.wsId]}</span>
            {/if}
          </span>
        {/if}
      {/each}
    </span>
    {#each sense.exampleSentences as example (example.id)}
      {@const usedVernacular = wsService.vernacular.filter(ws => !!example.sentence[ws.wsId])}
      {@const usedAnalysis = wsService.analysis.filter(ws => !!example.translation[ws.wsId])}
      {#if usedVernacular.length || usedAnalysis.length}
        <span class="-mr-0.5">[</span>
          {#each usedVernacular as ws}
            <span class={wsService.wsColor(ws.wsId, 'vernacular')}>{example.sentence[ws.wsId]}</span>
            <span></span><!-- standardizes whitespace between texts -->
          {/each}
          <span></span>
          {#each usedAnalysis as ws}
            <span class={wsService.wsColor(ws.wsId, 'analysis')}>{example.translation[ws.wsId]}</span>
            <span></span><!-- standardizes whitespace between texts -->
          {/each}
        <span class="-ml-0.5">]</span>
        <span></span><!-- standardizes whitespace between texts -->
      {/if}
    {/each}
  {/each}
</div>
