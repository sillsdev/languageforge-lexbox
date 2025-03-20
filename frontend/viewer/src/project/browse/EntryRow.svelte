<script lang="ts">
  import Badge from '$lib/components/ui/badge/badge.svelte';
  import type { IEntry } from '$lib/dotnet-types';
  import { useWritingSystemRunes } from '$lib/writing-system-runes.svelte';

  const { entry, isSelected = false, onclick, skeleton = false }: {
    entry?: IEntry;
    isSelected?: boolean;
    onclick?: () => void;
    skeleton?: boolean;
  } = $props();

  const writingSystemService = $derived(useWritingSystemRunes());
  const sensePreview = $derived(writingSystemService.firstDefOrGlossVal(entry?.senses?.[0]));
  const partOfSpeech = $derived(entry?.senses?.[0]?.partOfSpeech);

  // Generate random widths for skeleton UI elements
  function randomWidth(min: number, max: number): string {
    const percentage = Math.floor(Math.random() * (max - min + 1) + min);
    return `${percentage}%`;
  }

  // Generate random values when component is created
  const headwordWidth = randomWidth(20, 50);
  const definitionWidth = randomWidth(40, 80);
  const badgeWidth = randomWidth(10, 20);

  // Calculate animation delay based on index (staggered effect)
  const animationDelay = `${(Math.random() * 5) * 0.15}s`;
</script>

<button
  class="w-full px-4 py-3 text-left hover:bg-muted rounded"
  class:bg-muted={isSelected}
  class:cursor-default={skeleton}
  class:hover:bg-transparent={skeleton}
  onclick={skeleton ? undefined : onclick}
  disabled={skeleton}
>
  {#if skeleton || !entry}
    <div class="animate-pulse" style="animation-delay: {animationDelay}">
      <div class="h-5 bg-muted-foreground/20 rounded mb-2" style="width: {headwordWidth}"></div>
      <div class="h-4 bg-muted-foreground/20 rounded mb-2" style="width: {definitionWidth}"></div>
      <div class="h-6 bg-muted-foreground/20 rounded-full" style="width: {badgeWidth}"></div>
    </div>
  {:else}
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
  {/if}
</button>
