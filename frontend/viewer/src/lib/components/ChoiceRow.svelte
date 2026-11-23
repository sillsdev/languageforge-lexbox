<script lang="ts">
  import type {Snippet} from 'svelte';
  import type {HTMLButtonAttributes} from 'svelte/elements';
  import {Icon} from '$lib/components/ui/icon';
  import {cn} from '$lib/utils';

  type Props = HTMLButtonAttributes & {
    selected?: boolean;
    /** Starts something (play icon) rather than leading to a next step (chevron). */
    starts?: boolean;
    trailing?: Snippet;
    children: Snippet;
  };

  const {selected = false, starts = false, trailing, children, class: className, ...rest}: Props = $props();
</script>

<!-- A row in a bordered divide-y group, for choosing what to do next (not for data rows like entries). -->
<button
  type="button"
  aria-current={selected || undefined}
  {...rest}
  class={cn(
    'relative flex w-full items-center gap-3 px-4 py-3 text-start hover:bg-accent focus-visible:outline-none focus-visible:ring-[3px] focus-visible:ring-inset focus-visible:ring-ring/50',
    selected && 'bg-primary/10 dark:bg-primary/20',
    className,
  )}
>
  {#if selected}
    <!-- Inset from the ends so it never bends around a rounded first or last row. -->
    <span aria-hidden="true" class="bg-primary absolute inset-y-2 start-0 w-1 rounded-e-full"></span>
  {/if}
  <span class="flex min-w-0 grow flex-col">{@render children()}</span>
  {@render trailing?.()}
  <Icon
    icon={starts ? 'i-mdi-play' : 'i-mdi-chevron-right'}
    class={cn('shrink-0', !starts && 'rtl:rotate-180', selected || starts ? 'text-primary' : 'text-muted-foreground')}
    aria-hidden="true"
  />
</button>
