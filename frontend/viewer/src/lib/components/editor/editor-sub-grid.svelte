<script lang="ts">
  import {cn} from '$lib/utils';
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLAttributes} from 'svelte/elements';

  export type EditorSubGridProps = WithElementRef<HTMLAttributes<HTMLDivElement>>;

  const {class: className, children, ref = $bindable(null), ...restProps}: EditorSubGridProps = $props();
</script>

<div class={cn('editor-sub-grid grid grid-cols-subgrid col-span-full', className)} {...restProps}>
  {@render children?.()}
</div>
