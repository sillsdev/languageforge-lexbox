<script lang="ts">
  import {Icon} from '../icon';
  import {cn, type WithElementRef} from '$lib/utils.js';
  import type {HTMLLiAttributes} from 'svelte/elements';

  let {ref = $bindable(null), class: className, children, ...restProps}: WithElementRef<HTMLLiAttributes> = $props();
</script>

<li
  bind:this={ref}
  data-slot="breadcrumb-separator"
  role="presentation"
  aria-hidden="true"
  class={cn('*:[[class*="i-mdi-"]]:size-3.5', className)}
  {...restProps}
>
  {#if children}
    {@render children?.()}
  {:else}
    <Icon icon="i-mdi-chevron-right" />
  {/if}
</li>
