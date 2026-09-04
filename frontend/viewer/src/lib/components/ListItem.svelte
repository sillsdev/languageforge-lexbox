<script lang="ts" module>
  import {cn} from '$lib/utils';
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLButtonAttributes} from 'svelte/elements';
  import type {Snippet} from 'svelte';
  import type {IconClass} from '$lib/icon-class';

  export type ListItemProps = WithElementRef<HTMLButtonAttributes> & {
    selected?: boolean;
    skeleton?: boolean;
    loading?: boolean;
    icon?: Snippet | IconClass;
    actions?: Snippet;
    /** 'div' for a row that isn't clickable itself, because the things you click sit inside it. */
    element?: 'button' | 'div';
  };
</script>

<script lang="ts">
  import {Icon} from '$lib/components/ui/icon';

  let {
    class: className,
    ref = $bindable(null),
    children,
    icon = undefined,
    actions = undefined,
    selected = false,
    skeleton = false,
    disabled = false,
    loading = false,
    element = 'button',
    ...restProps
  }: ListItemProps = $props();

  const interactive = $derived(element === 'button');
</script>

<svelte:element
  this={element}
  aria-selected={selected && !skeleton && !disabled}
  disabled={interactive ? disabled || loading : undefined}
  data-skeleton={skeleton || undefined}
  class={cn(
    'w-full max-w-full px-4 py-3 flex text-left overflow-hidden items-center gap-4',
    'bg-muted rounded outline-none shadow-sm',
    'border-l-5 border-l-transparent aria-selected:border-l-primary',
    'aria-selected:bg-primary/15 aria-selected:dark:bg-primary/25',
    interactive && [
      'hover:shadow-md hover:z-10',
      'focus-visible:ring-[3px] focus-visible:ring-ring/50',
      'hover:bg-primary/15 dark:hover:bg-primary/25',
      'disabled:pointer-events-none disabled:contrast-[0.8]',
      'transition-transform active:scale-97',
    ],
    loading && 'animate-pulse',
    skeleton && 'cursor-default hover:bg-transparent pointer-events-none shadow-none',
    className,
  )}
  role={interactive ? 'row' : undefined}
  bind:this={ref}
  {...restProps}
>
  {#if typeof icon === 'string'}
    <Icon {icon} class="size-6" />
  {:else}
    {@render icon?.()}
  {/if}
  <!-- min-w-0: let long unbreakable content wrap rather than stretch the item past its container. -->
  <div class="flex min-w-0 grow flex-col">
    {@render children?.()}
  </div>
  {@render actions?.()}
</svelte:element>
