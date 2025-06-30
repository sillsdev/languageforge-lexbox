<script lang="ts">
  import Anchor from '$lib/components/ui/anchor/anchor.svelte';
  import {cn} from '$lib/utils';
  import {mergeProps} from 'bits-ui';
  import type {HTMLAttributes} from 'svelte/elements';
  import type {HTMLAnchorAttributes} from 'svelte/elements';

  type Props = HTMLAttributes<HTMLButtonElement> & HTMLAnchorAttributes & {
    href?: string | undefined;
    disabled?: boolean;
  }

  let { href, children, disabled, ...rest }: Props = $props();

  const mergedProps = $derived(mergeProps({
    class: cn('button-list-item w-full text-start', disabled && 'pointer-events-none'),
    role: 'button',
    tabindex: 0,
    disabled,
  }, rest));
</script>

{#snippet content()}
  <div></div>
  {@render children?.()}
  <div></div>
{/snippet}

{#if href}
  <Anchor {href} children={content} {...mergedProps} />
{:else}
  <button {...mergedProps} type="button">
    {@render content()}
  </button>
{/if}

<style lang="postcss">
  .button-list-item {
    &:first-child :global(.ListItem) {
      @apply border-t-0 rounded-t;
    }
    &:last-child :global(.ListItem) {
      @apply rounded-b;
    }
  }
</style>
