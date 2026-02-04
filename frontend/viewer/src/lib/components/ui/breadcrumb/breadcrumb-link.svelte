<script lang="ts">
  import {cn} from '$lib/utils.js';
  import type {Snippet} from 'svelte';
  import type {HTMLAnchorAttributes} from 'svelte/elements';
  import Anchor from '../anchor/anchor.svelte';

  let {
    ref = $bindable(null),
    class: className,
    href = undefined,
    child,
    children,
    ...restProps
  }: HTMLAnchorAttributes & {
    ref?: HTMLElement | null;
    child?: Snippet<[{props: HTMLAnchorAttributes}]>;
  } = $props();

  const attrs = $derived({
    class: cn('hover:text-foreground transition-colors', className),
    href,
    ...restProps,
  });
</script>

{#if child}
  {@render child({props: attrs})}
{:else}
  <Anchor bind:ref {...attrs}>
    {@render children?.()}
  </Anchor>
{/if}
