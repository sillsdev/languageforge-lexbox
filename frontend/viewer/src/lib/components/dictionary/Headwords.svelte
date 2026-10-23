<script lang="ts">
  import type {IEntry, IWritingSystem} from '$lib/dotnet-types';
  import {cn} from '$lib/utils';
  import type {HTMLAttributes} from 'svelte/elements';
  import {useWritingSystemService} from '$project/data';
  import {useViewService} from '$lib/views/view-service.svelte';

  let {
    entry,
    class: className,
    placeholder,
    respectView = true,
    ...restProps
  }: HTMLAttributes<HTMLElement> & {
    entry: IEntry;
    placeholder?: string;
    /** false shows every text vernacular, not just the current view's */
    respectView?: boolean;
  } = $props();

  const wsService = useWritingSystemService();
  const viewService = useViewService();

  function headwordsIn(writingSystems: IWritingSystem[]) {
    return writingSystems
      .map((ws) => ({
        wsId: ws.wsId,
        value: wsService.headword(entry, ws.wsId),
        color: wsService.wsColor(ws.wsId, 'vernacular'),
      }))
      .filter(({value}) => !!value);
  }

  const viewHeadwords = $derived(
    respectView
      ? headwordsIn(wsService.viewVernacularNoAudio(viewService.currentView))
      : headwordsIn(wsService.vernacularNoAudio),
  );
  // Nothing in the view's writing systems: show the forms the entry does have; their colours say which
  const headwords = $derived(viewHeadwords.length ? viewHeadwords : headwordsIn(wsService.vernacularNoAudio));
</script>

<!-- wrap-break-word: a headword can be one long unbreakable word, which otherwise overflows its container. -->
<strong class={cn('wrap-break-word', className)} {...restProps}>
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
