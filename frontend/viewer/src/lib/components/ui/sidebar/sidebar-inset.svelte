<script lang="ts">
  import {cn, type WithElementRef} from '$lib/utils.js';
  import type {HTMLAttributes} from 'svelte/elements';

  let {
    ref = $bindable(null),
    class: className,
    children,
    ...restProps
  }: WithElementRef<HTMLAttributes<HTMLElement>> = $props();
</script>

<main
  bind:this={ref}
  data-slot="sidebar-inset"
  class={cn(
    // min-w-0: without it the inset can't shrink below its content's min-content width,
    // so wide content pushes the whole page past the viewport instead of clipping.
    'bg-background relative flex w-full min-w-0 flex-1 flex-col',
    'lg:peer-data-[variant=inset]:m-2 lg:peer-data-[variant=inset]:ms-0 lg:peer-data-[variant=inset]:rounded-xl lg:peer-data-[variant=inset]:shadow-sm lg:peer-data-[variant=inset]:peer-data-[state=collapsed]:ms-2',
    className,
  )}
  {...restProps}
>
  {@render children?.()}
</main>
