<script lang="ts">
  import {cn} from '$lib/utils';
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLAttributes} from 'svelte/elements';


  type FieldBodyProps = WithElementRef<HTMLAttributes<HTMLDivElement>>;

  const {
    class: className,
    children,
    ref = $bindable(null),
    ...restProps
  }: FieldBodyProps = $props();
</script>

<div class={cn('grid grid-cols-subgrid col-span-full @3xl:col-start-2 @3xl:col-end-4', className)} {...restProps}>
  {@render children?.()}
</div>
