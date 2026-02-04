<script lang="ts">
  import {cn} from '$lib/utils';
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLAttributes} from 'svelte/elements';

  type EditorGridProps = WithElementRef<HTMLAttributes<HTMLDivElement>>;

  let {class: className, children, ref = $bindable(null), ...restProps}: EditorGridProps = $props();
</script>

<div
  class={cn(
    'grid gap-4',
    '            grid-cols-[fit-content(70px)_1fr]',
    ' @lg/editor:grid-cols-[fit-content(70px)_1fr]',
    '@3xl/editor:grid-cols-[180px_fit-content(80px)_1fr]',
    className,
  )}
  {...restProps}
  bind:this={ref}
>
  {@render children?.()}
</div>
