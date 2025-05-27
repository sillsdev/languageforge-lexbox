<script lang="ts">
  import DictionaryEntry from '$lib/DictionaryEntry.svelte';
  import ListItem, {type ListItemProps} from '$lib/components/ListItem.svelte';
  import Badge from '$lib/components/ui/badge/badge.svelte';
  import type {IEntry, ISense} from '$lib/dotnet-types';
  import {usePartsOfSpeech} from '$lib/parts-of-speech.svelte';
  import {useWritingSystemService} from '$lib/writing-system-service.svelte';
  import {mergeProps, type WithoutChildrenOrChild} from 'bits-ui';
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
  const partOfSpeechService = usePartsOfSpeech();

  const senseSummaries = $derived(entry?.senses.map(getSummary) ?? []);
  let displaySummaryCount = $state(2);

  type SenseSummary = {
    id: string;
    preview: string;
    partOfSpeech: string | undefined;
  }

  function getSummary(sense: ISense): SenseSummary {
    return {
      id: sense.id,
      preview: getPreview(sense),
      partOfSpeech: sense.partOfSpeech ? partOfSpeechService.getLabel(sense.partOfSpeech) : undefined,
    };
  }

  function getPreview(sense: ISense): string {
    let gloss = writingSystemService.firstGloss(sense);
    let definition = writingSystemService.firstDef(sense);

    if (gloss && definition) {
      return `${gloss}; ${definition}`;
    }
    return gloss || definition;
  }

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

<ListItem {...mergeProps(rest, { class: 'gap-2' })}>
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
    {#each senseSummaries.slice(0, displaySummaryCount) as {preview, partOfSpeech, id}, i (id)}
      {@const isLast = i === displaySummaryCount - 1}
      {@const rest = senseSummaries.length - displaySummaryCount}
      {#if isLast && rest}
        <div class="flex items-end justify-between gap-2">
          <div class="grow bg-foreground/5 mt-2 _text-background pl-96 pt-2_ -ml-96 -mr-4 -mb-96 pb-[23.25rem] shrink rounded flex gap-2 justify-between items-center flex-nowrap">
            <span class="line-clamp-1">{preview}</span>
            <div class="flex items-center gap-2 flex-nowrap shrink-0">
              {#if partOfSpeech}
                <Badge class="bg-primary/60">{partOfSpeech}</Badge>
              {/if}
              <span class="bg-foreground/5 p-2 whitespace-nowrap">
                {$t`+${rest}`}
              </span>
            </div>
          </div>
        </div>
      {:else}
        <div class="grow bg-foreground/5 mt-2 _text-background px-96 pt-2 -mx-96 -mb-96 pb-[23.5rem] shrink rounded flex justify-between items-center flex-nowrap">
          <span class="line-clamp-1">{preview}</span>
          {#if partOfSpeech}
            <Badge class="bg-primary/60">{partOfSpeech}</Badge>
          {/if}
        </div>
      {/if}
    {/each}
  {/if}
</ListItem>

