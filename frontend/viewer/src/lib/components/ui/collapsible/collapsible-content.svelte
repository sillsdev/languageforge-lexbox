<script lang="ts">
  import {Collapsible as CollapsiblePrimitive, type WithoutChildrenOrChild} from 'bits-ui';
  import {slide} from 'svelte/transition';
  import type {Snippet} from 'svelte';
  let {
    ref = $bindable(null),
    duration = 150,
    children,
    ...restProps
  }: WithoutChildrenOrChild<CollapsiblePrimitive.ContentProps> & {
    duration?: number;
    children?: Snippet;
  } = $props();
</script>

<CollapsiblePrimitive.Content forceMount {...restProps} bind:ref>
  {#snippet child({props, open})}
    {#if open}
      <div {...props} transition:slide={{duration}}>
        {@render children?.()}
      </div>
    {/if}
  {/snippet}
</CollapsiblePrimitive.Content>
