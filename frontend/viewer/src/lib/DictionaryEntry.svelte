<script lang="ts">
  import type { IEntry } from './mini-lcm';
  import { firstDefOrGlossVal, firstVal } from './utils';

  export let entry: IEntry;

  export let lines: number;

  $: lines = entry.senses.length > 1 ? entry.senses.length + 1 : 1;
</script>

<strong>
  {Object.values(entry.lexemeForm)
    .filter((value) => !!value)
    .join(' / ')}
</strong>
{#each entry.senses as sense, i}
  {#if entry.senses.length > 1}
    <br />
    <strong>{i + 1})</strong>
  {/if}
  <i>{sense.partOfSpeech}</i>
  {firstDefOrGlossVal(sense)}
  {#each sense.exampleSentences as example}
    <i>{firstVal(example.sentence)}</i>
    {firstVal(example.translation)}&nbsp;
  {/each}
{/each}
