<script lang="ts">
  import type { Snippet } from 'svelte';
  import { createBubbler } from 'svelte/legacy';

  const bubble = createBubbler();
  import Loader from '$lib/components/Loader.svelte';

  interface Props {
    loading?: boolean;
    active?: boolean;
    variant?: 'btn-primary' | 'btn-success' | 'btn-error' | 'btn-ghost' | 'btn-warning' | 'btn-accent' | undefined;
    outline?: boolean;
    type?: undefined | 'submit';
    size?: undefined | 'btn-sm';
    disabled?: boolean;
    customLoader?: boolean;
    children?: Snippet;
    onclick: () => void;
    [key: string]: unknown;
  }

  let {
    loading = false,
    active = false,
    variant = undefined,
    outline = false,
    type = undefined,
    size = undefined,
    disabled = false,
    customLoader = false,
    children,
    onclick,
    ...rest
  }: Props = $props();
</script>

<!-- https://daisyui.com/components/button -->
<button
  {onclick}
  {...rest}
  class="btn whitespace-nowrap {variant ?? ''} {rest.class ?? ''} {size ?? ''}"
  {type}
  class:btn-outline={outline}
  class:btn-active={active}
  disabled={disabled && !loading}
  class:pointer-events-none={loading || (rest.class as string)?.includes('pointer-events-none')}
>
  {#if !customLoader}
    <Loader {loading} />
  {/if}
  {@render children?.()}
</button>
