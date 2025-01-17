<script lang="ts">
  import type { IEntry } from '$lib/dotnet-types';
  import { firstDefOrGlossVal, firstSentenceOrTranslationVal, headword } from '../utils';
  import {useCurrentView} from '$lib/services/view-service';
  import {fieldName} from '$lib/i18n';

  export let entry: IEntry | undefined;
  const currentView = useCurrentView();
</script>

{#if entry}
  {@const _headword = headword(entry) ?? ''}
  <div class="border flex flex-col rounded-md overflow-auto">
    <a class="toc-item px-2" href="{location}/#entry" title={_headword}><span class="opacity-80 mr-1">{fieldName({id: 'entry'}, $currentView.i18nKey)}:</span>{_headword || '—'}</a>
    {#each entry.senses as sense, i (sense.id)}
      {@const _sense = firstDefOrGlossVal(sense) ?? ''}
      <a class="toc-item px-2 pl-4" href="{location}/#sense{i + 1}" title={_sense}><span class="opacity-80 mr-1">{fieldName({id: 'sense'}, $currentView.i18nKey)}:</span>{_sense || '—'}</a>
      {#each sense.exampleSentences as example, j (example.id)}
        {@const _example = firstSentenceOrTranslationVal(example) ?? ''}
        <a class="toc-item px-2 pl-6 text-sm" href="{location}/#example{i + 1}-{j + 1}" title={_example}><span class="opacity-80 mr-1">Example:</span>{_example || '—'}</a>
      {/each}
    {/each}
  </div>
{/if}

<style lang="postcss">
  .toc-item {
    @apply py-1;
    flex-shrink: 0;

    &:hover {
      @apply bg-surface-300;
    }

    white-space: nowrap;
    overflow-x: clip;
    text-overflow: ellipsis;
  }
</style>
