<script lang="ts">
  import type { IEntry, ISense } from '$lib/dotnet-types';
  import {asString, useWritingSystemService} from './writing-system-service.svelte';
  import { usePartsOfSpeech } from './parts-of-speech.svelte';
  import type {HTMLAttributes} from 'svelte/elements';
  import {Icon} from '$lib/components/ui/icon';
  import {cn} from '$lib/utils';
  import type {Snippet} from 'svelte';
  import {Link} from 'svelte-routing';
  let {
    entry,
    showLinks = false,
    lines = $bindable(),
    actions,
    class: className,
    ...restProps
  }: HTMLAttributes<HTMLDivElement> & {
    entry: IEntry,
    showLinks?: boolean,
    lines?: number,
    actions?: Snippet
  } = $props();

  $effect(() => {
    lines = entry.senses.length > 1 ? entry.senses.length + 1 : 1;
  });

  const wsService = useWritingSystemService();

  let headwords = $derived.by(() => {
    return wsService.vernacular
      .map(ws => ({
        wsId: ws.wsId,
        value: wsService.headword(entry, ws.wsId),
        color: wsService.wsColor(ws.wsId, 'vernacular'),
      }))
      .filter(({value}) => !!value);
  });

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
          ...wsService.vernacular.map(ws => ({
            text: asString(example.sentence[ws.wsId]),
            color: wsService.wsColor(ws.wsId, 'vernacular'),
          })),
          ...wsService.analysis.map(ws => ({
            text: asString(example.translation[ws.wsId]),
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
    <Link to={`${location.href.replace(location.hash, '')}#sense${index+1}`} class="font-bold group/sense underline">
      <Icon icon="i-mdi-link" class={cn(
          'opacity-0',
          'group-hover/sense:opacity-100 group-hover/sense:visible transition-all',
          'size-4 align-sub'
        )}/><span class="ml-[2px]">{index + 1}</span>
    </Link>
  {:else}
    <span class="font-bold">{index + 1}</span>
  {/if}
  {' · '}
{/snippet}

<div class={cn('group/container', className)} {...restProps}>
  <div class="float-right group-[&:not(:hover)]/container:invisible relative -top-1">
    {@render actions?.()}
  </div>
  <strong class="mr-1">
    {#each headwords as headword, i (headword.wsId)}
      {#if i > 0}{' / '}{/if}
      <span class={headword.color}>{headword.value}</span>
    {/each}
  </strong>
  {#each senses as sense, i (sense.id)}
    {#if senses.length > 1}
      <br />
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
        {''}
      {/each}
    </span>
    {#each sense.exampleSentences as example (example.id)}
      {#each example.sentences as sentence, j}
        {@const first = j === 0}
        {@const last = j === example.sentences.length - 1}
        {#if j > 0};{/if}
        {#if first}[{/if}<span class={sentence.color}>{sentence.text}</span>{#if last}]{/if}
      {/each}
    {/each}
  {/each}
</div>
