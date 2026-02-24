<script lang="ts">
  import {cn} from '$lib/utils.js';
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLLiAttributes} from 'svelte/elements';

  let {ref = $bindable(null), class: className, children, ...restProps}: WithElementRef<HTMLLiAttributes> = $props();
</script>

<li bind:this={ref} class={cn('inline-flex items-center gap-1.5', className)} {...restProps}>
  {@render children?.()}
</li>
