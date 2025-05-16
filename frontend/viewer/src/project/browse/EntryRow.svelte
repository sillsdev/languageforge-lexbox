<script lang="ts">
  import Badge from '$lib/components/ui/badge/badge.svelte';
  import type { IEntry } from '$lib/dotnet-types';
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';
  import type {Snippet} from 'svelte';
  import {t} from 'svelte-i18n-lingui';
  import DictionaryEntry from '$lib/DictionaryEntry.svelte';
  import ListItem from '$lib/components/ListItem.svelte';
  import {cn} from '$lib/utils';

  const { entry, isSelected = false, onclick, skeleton = false, badge = undefined, previewDictionary = false }: {
    entry?: IEntry;
    isSelected?: boolean;
    onclick?: () => void;
    skeleton?: boolean;
    badge?: Snippet,
    previewDictionary?: boolean;
  } = $props();

  const writingSystemService = useWritingSystemService();
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

<ListItem
  aria-selected={isSelected}
  class={cn(skeleton && 'cursor-default hover:bg-transparent')}
  onclick={skeleton ? undefined : onclick}
  disabled={skeleton}
>
  {#if skeleton || !entry}
    <div class="animate-pulse" style="animation-delay: {animationDelay}">
      <div class="h-5 bg-muted-foreground/20 rounded mb-2" style="width: {headwordWidth}"></div>
      <div class="h-4 bg-muted-foreground/20 rounded mb-2" style="width: {definitionWidth}"></div>
      <div class="h-6 bg-muted-foreground/20 rounded-full" style="width: {badgeWidth}"></div>
    </div>
  {:else if previewDictionary}
    <DictionaryEntry {entry}/>
  {:else}
    <h2 class="font-medium text-2xl">{writingSystemService.headword(entry) || $t`Untitled`}</h2>
    <div class="flex flex-row items-start justify-between gap-2">
      {#if sensePreview}
      <div class="text-sm text-muted-foreground">
        {sensePreview}
      </div>
        {#if badge}
          {@render badge()}
        {:else if partOfSpeech}
          <Badge variant="default" class="bg-primary/60">
            {writingSystemService.pickBestAlternative(partOfSpeech.name, 'analysis')}
          </Badge>
        {/if}
    {/if}
    </div>

  {/if}
</ListItem>
