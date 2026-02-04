<script lang="ts">
  import {cn} from '$lib/utils.js';
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLLabelAttributes} from 'svelte/elements';

  type Props = WithElementRef<HTMLLabelAttributes>;

  let {ref = $bindable(null), class: className, children, ...restProps}: Props = $props();
</script>

<label
  bind:this={ref}
  class={cn('text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70', className)}
  {...restProps}
>
  {@render children?.()}
</label>
