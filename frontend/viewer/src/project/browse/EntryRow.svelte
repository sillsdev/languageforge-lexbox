<script lang="ts">
  import DictionaryEntry from '$lib/DictionaryEntry.svelte';
  import ListItem, {type ListItemProps} from '$lib/components/ListItem.svelte';
  import Badge from '$lib/components/ui/badge/badge.svelte';
  import type {IEntry} from '$lib/dotnet-types';
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';
  import type {WithoutChildrenOrChild} from 'bits-ui';
  import type {Snippet} from 'svelte';
  import {t} from 'svelte-i18n-lingui';

  interface Props extends WithoutChildrenOrChild<ListItemProps> {
    entry?: IEntry;
    badge?: Snippet;
    previewDictionary?: boolean;
  };

  const {
    entry,
    badge,
    previewDictionary = false,
    ...rest
  }: Props = $props();

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

<ListItem {...rest}>
  {#if rest.skeleton || !entry}
    <div class="animate-pulse" style="animation-delay: {animationDelay}">
      <div class="h-5 bg-muted-foreground/20 rounded mb-2" style="width: {headwordWidth}"></div>
      <div class="h-4 bg-muted-foreground/20 rounded mb-2" style="width: {definitionWidth}"></div>
      <div class="h-6 bg-muted-foreground/20 rounded-full" style="width: {badgeWidth}"></div>
    </div>
  {:else if previewDictionary}
    <DictionaryEntry {entry}/>
  {:else}
    <h2 class="font-medium text-2xl flex justify-between items-center">
      {writingSystemService.headword(entry) || $t`Untitled`}
      {@render badge?.()}
    </h2>
    {#if entry.senses.length}
      <div class="flex justify-between items-end">
        <div class="text-sm text-muted-foreground">
          {sensePreview}
        </div>
        <Badge variant="default" class="bg-primary/60 whitespace-nowrap">
          {#if partOfSpeech}
            {writingSystemService.pickBestAlternative(partOfSpeech.name, 'analysis')}
            {#if entry.senses.length > 1}
              + {entry.senses.length - 1}
            {/if}
          {:else}
            {entry.senses.length}
          {/if}
        </Badge>
      </div>
    {/if}
  {/if}
</ListItem>
