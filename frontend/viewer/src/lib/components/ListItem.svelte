<script lang="ts" module>
  import {cn} from '$lib/utils';
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLButtonAttributes} from 'svelte/elements';
  import type {Snippet} from 'svelte';

  export type ListItemProps = WithElementRef<HTMLButtonAttributes> & {
    selected?: boolean;
    skeleton?: boolean;
    icon?: Snippet | string;
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
    ...restProps
  }: ListItemProps = $props();
</script>

<button
  aria-selected={selected && !skeleton && !disabled}
  disabled={disabled || skeleton}
  class={cn(
    'w-full max-w-full px-4 py-3 flex text-left overflow-hidden items-center gap-4',
    'dark:bg-muted/50 bg-muted/80 hover:bg-primary/15 hover:dark:bg-primary/15 aria-selected:ring-2 ring-primary ring-offset-background rounded',
    'disabled:pointer-events-none disabled:bg-destructive/5',
    skeleton && 'cursor-default hover:bg-transparent',
    className)}
  role="row"
  bind:this={ref}
  {...restProps}
>
  {#if typeof icon === 'string'}
    <Icon {icon} class="size-6"/>
  {:else}
    {@render icon?.()}
  {/if}
  <div class="flex flex-col grow">
    {@render children?.()}
  </div>
  {@render actions?.()}

</button>
