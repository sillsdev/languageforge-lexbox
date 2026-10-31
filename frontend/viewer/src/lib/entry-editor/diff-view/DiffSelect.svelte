<script lang="ts" generics="T">
  import {t} from 'svelte-i18n-lingui';
  import {Icon} from '$lib/components/ui/icon';
  import DiffShell from './DiffShell.svelte';
  import DiffMarker from './DiffMarker.svelte';
  import {diffAdded, diffEmpty, diffRemoved} from './diff-classes';

  let {before, after, idSelector, labelSelector}: {
    before?: T | null;
    after?: T | null;
    idSelector: (item: T) => string;
    labelSelector: (item: T) => string;
  } = $props();

  const changed = $derived((before ? idSelector(before) : undefined) !== (after ? idSelector(after) : undefined));

  function label(item: T): string {
    return labelSelector(item) || $t`Untitled`;
  }
</script>

<DiffShell class="items-center">
  {#if !changed}
    {#if after}
      <span class="text-foreground">{label(after)}</span>
    {:else}
      <span class={diffEmpty}>&nbsp;</span>
    {/if}
  {:else}
    <span class="inline-flex flex-wrap items-center gap-1.5">
      {#if before}
        <span class="rounded-sm px-1 {diffRemoved}"><DiffMarker state="removed" />{label(before)}</span>
      {/if}
      {#if before && after}
        <Icon icon="i-mdi-arrow-right" class="size-4 shrink-0 text-muted-foreground" />
      {/if}
      {#if after}
        <span class="rounded-sm px-1 {diffAdded}"><DiffMarker state="added" />{label(after)}</span>
      {/if}
    </span>
  {/if}
</DiffShell>
