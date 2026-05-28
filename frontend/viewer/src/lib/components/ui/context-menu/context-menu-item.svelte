<script lang="ts">
  import {cn} from '$lib/utils.js';
  import {ContextMenu as ContextMenuPrimitive} from 'bits-ui';

  let {
    ref = $bindable(null),
    class: className,
    inset,
    variant = 'default',
    ...restProps
  }: ContextMenuPrimitive.ItemProps & {
    inset?: boolean;
    variant?: 'default' | 'destructive';
  } = $props();
</script>

<ContextMenuPrimitive.Item
  bind:ref
  data-slot="context-menu-item"
  data-inset={inset}
  data-variant={variant}
  class={cn(
    "data-highlighted:bg-accent data-highlighted:text-accent-foreground data-[variant=destructive]:text-destructive data-[variant=destructive]:data-highlighted:bg-destructive/10 dark:data-[variant=destructive]:data-highlighted:bg-destructive/20 data-[variant=destructive]:data-highlighted:text-destructive data-[variant=destructive]:*:[[class*='i-mdi-']]:text-destructive! [&>[class*='i-mdi-']:not([class*='text-'])]:text-muted-foreground relative flex cursor-pointer items-center gap-2 rounded-sm px-2 py-1.5 text-sm outline-hidden select-none data-disabled:pointer-events-none data-disabled:opacity-50 data-inset:ps-8 **:[[class*='i-mdi-']]:pointer-events-none *:[[class*='i-mdi-']]:shrink-0 [&_[class*='i-mdi-']:not([class*='size-'])]:size-4",
    className,
  )}
  {...restProps}
/>
