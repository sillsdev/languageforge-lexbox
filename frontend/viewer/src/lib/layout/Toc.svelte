<script lang="ts">
  import type { IEntry } from '$lib/dotnet-types';
  import {useCurrentView} from '$lib/views/view-service';
  import {fieldName} from '$lib/i18n';
  import {useWritingSystemService} from '$lib/writing-system-service';

  export let entry: IEntry | undefined;
  const currentView = useCurrentView();
  const writingSystemService = useWritingSystemService();
</script>

{#if entry}
  {@const _headword = writingSystemService.headword(entry) ?? ''}
  {@const locationWithoutHash = window.location.href.split('#')[0]}
  <div class="border flex flex-col rounded-md overflow-auto">
    <a class="toc-item px-2" href="{locationWithoutHash}#entry" title={_headword}><span class="opacity-80 mr-1">{fieldName({id: 'entry'}, $currentView.i18nKey)}:</span>{_headword || '—'}</a>
    {#each entry.senses as sense, i (sense.id)}
      {@const _sense = writingSystemService.firstDefOrGlossVal(sense) ?? ''}
      <a class="toc-item px-2 pl-4" href="{locationWithoutHash}#sense{i + 1}" title={_sense}><span class="opacity-80 mr-1">{fieldName({id: 'sense'}, $currentView.i18nKey)}:</span>{_sense || '—'}</a>
      {#each sense.exampleSentences as example, j (example.id)}
        {@const _example = writingSystemService.firstSentenceOrTranslationVal(example) ?? ''}
        <a class="toc-item px-2 pl-6 text-sm" href="{locationWithoutHash}#example{i + 1}-{j + 1}" title={_example}><span class="opacity-80 mr-1">Example:</span>{_example || '—'}</a>
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
