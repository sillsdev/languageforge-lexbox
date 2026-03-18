<script lang="ts">
  import {cn} from '$lib/utils.js';
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLAttributes} from 'svelte/elements';

  let {ref = $bindable(null), class: className, children, ...restProps}: WithElementRef<HTMLAttributes<HTMLDivElement>> = $props();
</script>

<div
  bind:this={ref}
  data-slot="checkbox-group"
  class={cn('grid md:gap-4', className)}
  {...restProps}
>
  {@render children?.()}
</div>
