<script lang="ts">
  import Badge from '$lib/components/ui/badge/badge.svelte';
  import type { IEntry } from '$lib/dotnet-types';
  import { useWritingSystemRunes } from '$lib/writing-system-runes.svelte';

  const { entry, isSelected = false, onclick }: {
    entry: IEntry;
    isSelected?: boolean;
    onclick: () => void;
  } = $props();

  const writingSystemService = $derived(useWritingSystemRunes());
  const sensePreview = $derived(writingSystemService.firstDefOrGlossVal(entry.senses?.[0]));
  const partOfSpeech = $derived(entry.senses?.[0]?.partOfSpeech);
</script>

<button
  class="w-full px-4 py-3 text-left hover:bg-muted rounded"
  class:bg-muted={isSelected}
  {onclick}
>
  <div class="font-medium">{writingSystemService.headword(entry) || 'Untitled'}</div>
  {#if sensePreview}
    <div class="text-sm text-muted-foreground">
      {sensePreview}
    </div>
    {#if partOfSpeech}
      <Badge>
        {writingSystemService.pickBestAlternative(partOfSpeech.name, 'analysis')}
      </Badge>
    {/if}
  {/if}
</button>
