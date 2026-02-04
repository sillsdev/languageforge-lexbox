<script lang="ts">
  import {cn} from '$lib/utils';
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLAttributes} from 'svelte/elements';

  type FieldBodyProps = WithElementRef<HTMLAttributes<HTMLDivElement>> & {
    subGrid?: boolean;
  };

  const {class: className, children, subGrid = false, ref = $bindable(null), ...restProps}: FieldBodyProps = $props();
</script>

<div
  class={cn(
    'grid grid-cols-1 col-span-full @3xl/editor:col-start-2 @3xl/editor:col-end-4',
    subGrid ? 'grid-cols-subgrid' : 'grid-cols-1',
    className,
  )}
  {...restProps}
>
  {@render children?.()}
</div>
