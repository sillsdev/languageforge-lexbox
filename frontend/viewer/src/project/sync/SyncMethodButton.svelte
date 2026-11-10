<script lang="ts">
  import type {Snippet} from 'svelte';
  import {Icon} from '$lib/components/ui/icon';
  import {cn} from '$lib/utils';

  interface Props {
    title: string;
    icon: Snippet;
    description?: Snippet;
    /** The main sync method: stands apart from the list of the others */
    featured?: boolean;
    onclick: () => void;
  }

  const {title, icon, description, featured = false, onclick}: Props = $props();
</script>

<button type="button" {onclick}
  class={cn('flex w-full items-center gap-4 px-4 py-3 text-start hover:bg-accent focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-ring/50',
    featured && 'rounded-lg border shadow-sm py-4')}>
  <span class="flex size-8 shrink-0 items-center justify-center">{@render icon()}</span>
  <span class="flex min-w-0 grow flex-col">
    <span class={featured ? 'text-base font-semibold' : 'font-medium'}>{title}</span>
    {#if description}
      <span class="text-sm text-muted-foreground">{@render description()}</span>
    {/if}
  </span>
  <Icon icon="i-mdi-chevron-right" class="shrink-0 text-muted-foreground rtl:rotate-180" />
</button>
