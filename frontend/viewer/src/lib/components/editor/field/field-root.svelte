<script lang="ts">
  import {cn} from '$lib/utils';
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLAttributes} from 'svelte/elements';


  type FieldRootProps = WithElementRef<HTMLAttributes<HTMLDivElement>>;

  const {
    class: className,
    children,
    ref = $bindable(null),
    ...restProps
  }: FieldRootProps = $props();
</script>

<div class={cn('grid grid-cols-subgrid col-span-full items-baseline', className)} {...restProps}>
  {@render children?.()}
</div>
