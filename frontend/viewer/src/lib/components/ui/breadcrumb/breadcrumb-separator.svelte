<script lang="ts">
  import {cn} from '$lib/utils.js';
  import type {WithElementRef} from 'bits-ui';
  import type {HTMLLiAttributes} from 'svelte/elements';
  import {Icon} from '../icon';

  let {ref = $bindable(null), class: className, children, ...restProps}: WithElementRef<HTMLLiAttributes> = $props();
</script>

<li role="presentation" aria-hidden="true" class={cn('[&>svg]:size-3.5', className)} bind:this={ref} {...restProps}>
  {#if children}
    {@render children?.()}
  {:else}
    <Icon icon="i-mdi-chevron-right" />
  {/if}
</li>
