<script lang="ts" module>
  import {cn} from '$lib/utils';
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLButtonAttributes} from 'svelte/elements';

  export type ListItemProps = WithElementRef<HTMLButtonAttributes> & {
    selected?: boolean;
    skeleton?: boolean;
  };
</script>

<script lang="ts">
  let {
    class: className,
    ref = $bindable(null),
    children,
    selected = false,
    skeleton = false,
    disabled = false,
    ...restProps
  }: ListItemProps = $props();
</script>

<button
  aria-selected={selected && !skeleton && !disabled}
  disabled={disabled || skeleton}
  class={cn(
    'w-full max-w-full px-4 py-3 flex flex-col items-stretch text-left overflow-hidden',
    'dark:bg-muted/50 bg-muted/80 hover:bg-primary/15 hover:dark:bg-primary/15 aria-selected:ring-2 ring-primary ring-offset-background rounded',
    'disabled:pointer-events-none disabled:bg-destructive/5',
    skeleton && 'cursor-default hover:bg-transparent',
    className)}
  role="row"
  bind:this={ref}
  {...restProps}
  >
  {@render children?.()}
</button>
