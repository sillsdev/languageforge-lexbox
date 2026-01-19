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
    ...restProps
  }: ListItemProps = $props();
</script>

<button
  aria-selected={selected && !skeleton && !disabled}
  selected={selected && !skeleton && !disabled ? 'true' : undefined}
  disabled={disabled || loading}
  data-skeleton={skeleton || undefined}
  class={cn(
    'w-full max-w-full px-4 py-3 flex text-left overflow-hidden items-center gap-4',
    'dark:bg-muted/50 bg-muted/80 hover:bg-primary/15 hover:dark:bg-primary/15 aria-selected:ring-2 ring-primary ring-offset-background rounded',
    'shadow hover:shadow-lg hover:z-10',
    'disabled:pointer-events-none disabled:contrast-[0.8]',
    loading && 'animate-pulse',
    skeleton && 'cursor-default hover:bg-transparent pointer-events-none shadow-none',
    className,
  )}
  role="row"
  bind:this={ref}
  {...restProps}
>
  {#if typeof icon === 'string'}
    <Icon {icon} class="size-6" />
  {:else}
    {@render icon?.()}
  {/if}
  <div class="flex flex-col grow">
    {@render children?.()}
  </div>
  {@render actions?.()}
</button>
