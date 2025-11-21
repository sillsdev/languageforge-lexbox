<script lang="ts">
  import type {IEntry} from '$lib/dotnet-types';
  import {cn} from '$lib/utils';
  import type {HTMLAttributes} from 'svelte/elements';
  import {useWritingSystemService} from '$project/data';

  let {
    entry,
    class: className,
    placeholder,
    ...restProps
  }: HTMLAttributes<HTMLElement> & {
    entry: IEntry;
    placeholder?: string;
  } = $props();

  const wsService = useWritingSystemService();

  let headwords = $derived.by(() => {
    return wsService.vernacular
      .filter((ws) => !ws.isAudio)
      .map((ws) => ({
        wsId: ws.wsId,
        value: wsService.headword(entry, ws.wsId),
        color: wsService.wsColor(ws.wsId, 'vernacular'),
      }))
      .filter(({ value }) => !!value);
  });
</script>

<strong class={cn(className)} {...restProps}>
  {#each headwords as headword, i (headword.wsId)}
    <!-- eslint-disable-next-line svelte/no-useless-mustaches This mustache is not useless, it preserves whitespace -->
    {#if i > 0}{' / '}{/if}
    <span class={headword.color}>{headword.value}</span>
  {:else}
    {#if placeholder}
      <span class="text-muted-foreground">{placeholder}</span>
    {/if}
  {/each}
</strong>
