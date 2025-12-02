<script lang="ts">
  import type { IEntry, ISense } from '$lib/dotnet-types';
  import {usePartsOfSpeech, asString, useWritingSystemService} from '$project/data';
  import type {HTMLAttributes} from 'svelte/elements';
  import {Icon} from '$lib/components/ui/icon';
  import {cn} from '$lib/utils';
  import type {Snippet} from 'svelte';
  import Headwords from './Headwords.svelte';
  let {
    entry,
    showLinks = false,
    lines = $bindable(),
    actions,
    class: className,
    headwordClass = '',
    highlightSenseId = undefined,
    hideExamples = false,
    ...restProps
  }: HTMLAttributes<HTMLDivElement> & {
    entry: IEntry,
    showLinks?: boolean,
    lines?: number,
    actions?: Snippet,
    headwordClass?: string,
    highlightSenseId?: string,
    hideExamples?: boolean,
  } = $props();

  $effect(() => {
    lines = entry.senses.length > 1 ? entry.senses.length + 1 : 1;
  });

  const wsService = useWritingSystemService();

  let senses = $derived(entry.senses.map(getRenderedContent));

  /**
   * Returns the rendered content for a sense.
   * Primary purpose is to filter out list items that won't be rendered,
   * so we can easily/reliably determine if an item is the last that will be rendered.
   * @param sense
   */
  function getRenderedContent(sense: ISense) {
    return {
      id: sense.id,
      partOfSpeech: partsOfSpeech.current.find(pos => pos.id === sense.partOfSpeechId)?.label,
      glossesAndDefs: wsService.analysis
        .filter(ws => !ws.isAudio)
        .map(ws => ({
          wsId: ws.wsId,
          wsAbbr: ws.abbreviation,
          gloss: sense.gloss[ws.wsId],
          definition: asString(sense.definition[ws.wsId]),
          color: wsService.wsColor(ws.wsId, 'analysis'),
        }))
        .filter(({gloss, definition}) => gloss || definition),
      exampleSentences: sense.exampleSentences.map(example => ({
        id: example.id,
        sentences: [
          ...wsService.vernacular
            .filter(ws => !ws.isAudio)
            .map(ws => ({
            text: asString(example.sentence[ws.wsId]),
            color: wsService.wsColor(ws.wsId, 'vernacular'),
          })),
          ...wsService.analysis
            .filter(ws => !ws.isAudio).map(ws => ({
            text: asString(example.translations[0]?.text?.[ws.wsId]),
            color: wsService.wsColor(ws.wsId, 'analysis'),
          })),
        ].filter(({text}) => !!text),
      })),
    }
  }

  const partsOfSpeech = usePartsOfSpeech();
</script>

{#snippet senseNumber(index: number)}
  {#if showLinks}
    <!-- eslint-disable-next-line svelte/no-navigation-without-resolve -->
    <a href={`${location.href.replace(location.hash, '')}#sense${index+1}`} class="font-bold group/sense underline">
      <Icon icon="i-mdi-link" class={cn(
          'opacity-0',
          'group-hover/sense:opacity-100 group-hover/sense:visible transition-all',
          'size-4 align-sub'
        )}/><span class="ml-[2px]">{index + 1}</span>
    </a>
  {:else}
    <span class="font-bold">{index + 1}</span>
  {/if}
  <!-- eslint-disable-next-line svelte/no-useless-mustaches This mustache is not useless, it preserves whitespace -->
  {' · '}
{/snippet}

<div class={cn('group/container', className)} {...restProps}>
  <div class="float-right group-[&:not(:hover)]/container:invisible relative -top-1">
    {@render actions?.()}
  </div>
  <Headwords {entry} class={cn('mr-1', headwordClass)} />
  {#each senses as sense, i (sense.id)}
    {#if senses.length > 1}
      <br/>
    {/if}
    <span class={cn(highlightSenseId === sense.id && 'rounded bg-secondary')}>
      {#if senses.length > 1}
        {@render senseNumber(i)}
      {/if}
      {#if sense.partOfSpeech}
        <i>{sense.partOfSpeech}</i>
      {/if}
      <span>
        {#each sense.glossesAndDefs as glossAndDef (glossAndDef.wsId)}
          <sub class="-mr-0.5">{glossAndDef.wsAbbr}</sub>
          {#if glossAndDef.gloss}
            <span class={glossAndDef.color}>{glossAndDef.gloss}</span>{#if glossAndDef.definition};{/if}
          {/if}
          {#if glossAndDef.definition}
            <span class={glossAndDef.color}>{glossAndDef.definition}</span>
          {/if}
          <!-- eslint-disable-next-line svelte/no-useless-mustaches This mustache is not useless, it is deliberately an empty string with no whitespace -->
          {''}
        {/each}
      </span>
      {#if !hideExamples}
        {#each sense.exampleSentences as example (example.id)}
        {#each example.sentences as sentence, j (sentence)}
          {@const first = j === 0}
          {@const last = j === example.sentences.length - 1}
          {#if j > 0};{/if}
          {#if first}[{/if}<span class={sentence.color}>{sentence.text}</span>{#if last}]{/if}
        {/each}
      {/each}
      {/if}
    </span>
  {/each}
</div>
