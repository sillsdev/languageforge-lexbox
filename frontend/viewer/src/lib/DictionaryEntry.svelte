<script lang="ts">
  import type { IEntry, ISense } from '$lib/dotnet-types';
  import {useWritingSystemService} from './writing-system-service.svelte';
  import { usePartsOfSpeech } from './parts-of-speech.svelte';

  export let entry: IEntry;
  export let lines: number = 0;

  $: lines = entry.senses.length > 1 ? entry.senses.length + 1 : 1;

  const wsService = useWritingSystemService();

  $: headwords = wsService.vernacular
    .map(ws => ({
      wsId: ws.wsId,
      value: wsService.headword(entry, ws.wsId),
      color: wsService.wsColor(ws.wsId, 'vernacular'),
    }))
    .filter(({value}) => !!value);

  $: senses = entry.senses.map(getRenderedContent);

  /**
   * Returns the rendered content for a sense.
   * Primary purpose is to filter out list items that won't be rendered,
   * so we can easily/reliably determine if an item is the last that will be rendered.
   * @param sense
   */
  function getRenderedContent(sense: ISense) {
    return {
      id: sense.id,
      partOfSpeech: $partsOfSpeech.find(pos => pos.id === sense.partOfSpeechId)?.label,
      glossesAndDefs: wsService.analysis
        .map(ws => ({
          wsId: ws.wsId,
          wsAbbr: ws.abbreviation,
          gloss: sense.gloss[ws.wsId],
          definition: sense.definition[ws.wsId],
          color: wsService.wsColor(ws.wsId, 'analysis'),
        }))
        .filter(({gloss, definition}) => gloss || definition),
      exampleSentences: sense.exampleSentences.map(example => ({
        id: example.id,
        sentences: [
          ...wsService.vernacular.map(ws => ({
            text: example.sentence[ws.wsId],
            color: wsService.wsColor(ws.wsId, 'vernacular'),
          })),
          ...wsService.analysis.map(ws => ({
            text: example.translation[ws.wsId],
            color: wsService.wsColor(ws.wsId, 'analysis'),
          })),
        ].filter(({text}) => !!text),
      })),
    }
  }

  const partsOfSpeech = usePartsOfSpeech();
</script>

<div>
  <strong class="inline-flex gap-1 mr-1">
    {#each headwords as headword, i (headword.wsId)}
      {#if i > 0}/{/if}
      <span class={headword.color}>{headword.value}</span>
    {/each}
  </strong>
  {#each senses as sense, i (sense.id)}
    {#if senses.length > 1}
      <br />
      <strong class="ml-2">{i + 1} · </strong>
    {/if}
    {#if sense.partOfSpeech}
      <i>{sense.partOfSpeech}</i>
    {/if}
    <span>
      {#each sense.glossesAndDefs as glossAndDef (glossAndDef.wsId)}
        <span class="ml-0.5">
          <sub class="-mr-0.5">{glossAndDef.wsAbbr}</sub>
          {#if glossAndDef.gloss}
            <span class={glossAndDef.color}>{glossAndDef.gloss}</span>{#if glossAndDef.definition};{/if}
          {/if}
          {#if glossAndDef.definition}
            <span class={glossAndDef.color}>{glossAndDef.definition}</span>
          {/if}
        </span>
      {/each}
    </span>
    {#each sense.exampleSentences as example (example.id)}
      {#each example.sentences as sentence, j}
        {@const first = j === 0}
        {@const last = j === example.sentences.length - 1}
        {#if j > 0};{/if}
        {#if first}[{/if}<span class={sentence.color}>{sentence.text}</span>{#if last}]{/if}
      {/each}
    {/each}
  {/each}
</div>
