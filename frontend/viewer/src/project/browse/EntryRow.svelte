<script lang="ts">
  import DictionaryEntry from '$lib/components/dictionary/DictionaryEntry.svelte';
  import ListItem, {type ListItemProps} from '$lib/components/ListItem.svelte';
  import Badge from '$lib/components/ui/badge/badge.svelte';
  import type {IEntry} from '$lib/dotnet-types';
  import {usePartsOfSpeech, useWritingSystemService} from '$project/data';
  import type {WithoutChildrenOrChild} from 'bits-ui';
  import type {Snippet} from 'svelte';
  import {t} from 'svelte-i18n-lingui';
  import {useViewService} from '$lib/views/view-service.svelte';

  interface Props extends WithoutChildrenOrChild<ListItemProps> {
    entry?: IEntry;
    badge?: Snippet;
    previewDictionary?: boolean;
    /** Writing system being sorted by; the shown headword prefers it. Undefined = default vernacular. */
    sortWs?: string;
  };

  let {
    entry,
    ref = $bindable(null),
    badge,
    previewDictionary = false,
    sortWs,
    ...rest
  }: Props = $props();

  const writingSystemService = useWritingSystemService();
  const partOfSpeechService = usePartsOfSpeech();
  const viewService = useViewService();
  const sensePreview = $derived(writingSystemService.viewFirstDefOrGlossVal(entry?.senses?.[0], viewService.currentView));
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

  const defaultWs = $derived(sortWs
    ?? writingSystemService.viewVernacularNoAudio(viewService.currentView)[0]?.wsId
    ?? '');
</script>

<ListItem bind:ref {...rest}>
  {#if rest.skeleton || !entry}
    <div>
      <div class="h-5 motion-safe:animate-shimmer rounded mb-2" style="width: {headwordWidth}"></div>
      <div class="h-4 motion-safe:animate-pulse bg-muted-foreground/20 rounded mb-2" style="width: {definitionWidth}; animation-delay: {animationDelay}"></div>
      <div class="h-6 motion-safe:animate-pulse bg-muted-foreground/20 rounded-full" style="width: {badgeWidth}; animation-delay: {animationDelay}"></div>
    </div>
  {:else if previewDictionary}
    <DictionaryEntry {entry}/>
  {:else}
    <!-- Show the WS abbreviation if the headword we display does not match what the user is expecting -->
    {@const best = writingSystemService.viewBestHeadwordIn(entry, viewService.currentView, defaultWs)}
    {@const fallbackWs = best && best.ws.wsId !== defaultWs ? best.ws : undefined}
    <h2 class="font-medium text-2xl flex min-w-0 justify-between items-center gap-2">
      <span class="min-w-0 wrap-break-word">
        {#if fallbackWs}
          <span class="-mr-1 relative -bottom-px text-xs font-normal text-foreground/80" title={fallbackWs.name}>{fallbackWs.abbreviation || fallbackWs.wsId}</span>
        {/if}
        {best?.value || $t`Untitled`}
      </span>
      {@render badge?.()}
    </h2>
    {#if entry.senses.length}
      <div class="flex justify-between items-end">
        <div class="text-sm text-muted-foreground">
          {sensePreview}
        </div>
        {#if partOfSpeech}
          <Badge variant="default" class="bg-primary/60 whitespace-nowrap">
            {partOfSpeechService.getLabel(partOfSpeech)}
          </Badge>
        {/if}
      </div>
    {/if}
  {/if}
</ListItem>
