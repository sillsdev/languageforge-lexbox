<script lang="ts">
  import {cn} from '$lib/utils';
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLAttributes} from 'svelte/elements';

  type EditorRootProps = WithElementRef<HTMLAttributes<HTMLDivElement>>;

  let {class: className, children, ref = $bindable(null), ...restProps}: EditorRootProps = $props();
</script>

<div class={cn('@container/editor', className)} {...restProps} bind:this={ref}>
  {@render children?.()}
</div>
