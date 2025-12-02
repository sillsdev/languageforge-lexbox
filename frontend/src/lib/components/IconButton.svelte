<script lang="ts">
  import type {IconString} from '$lib/icons';
  import Loader from './Loader.svelte';

  interface Props {
    // See https://icon-sets.iconify.design/mdi/
    icon: IconString;
    disabled?: boolean;
    loading?: boolean;
    active?: boolean;
    join?: boolean;
    variant?: 'btn-success' | 'btn-ghost' | 'btn-primary';
    size?: 'btn-sm';
    outline?: boolean;
    fake?: boolean; // for display purposes only
    onclick?: () => void;
    [key: string]: unknown;
  }

  const {
    icon,
    disabled = false,
    loading = false,
    active = false,
    join = false,
    variant,
    size,
    outline: _outline,
    fake = false,
    onclick,
    ...rest
  }: Props = $props();
  const outline = $derived(_outline ?? variant !== 'btn-ghost');
  const loadingSize = $derived(size === 'btn-sm' ? ('loading-xs' as const) : undefined);

  const xlIcons: IconString[] = ['i-mdi-refresh'];
  const textSize = $derived(xlIcons.includes(icon) ? 'text-xl' : 'text-lg');
</script>

<!-- type="button" ensures the button doen't act as a submit button when in a form -->
<button
  type="button"
  {onclick}
  disabled={disabled && !loading}
  class:pointer-events-none={fake || loading}
  class="btn btn-square {variant ?? ''} {size ?? ''} {rest.class ?? ''}"
  class:btn-outline={outline}
  class:btn-active={active}
  class:join-item={join}
>
  {#if !loading}
    <span class="{icon} {textSize}"></span>
  {:else}
    <Loader loading size={loadingSize} />
  {/if}
</button>
