<script lang="ts">
  import type { Snippet } from 'svelte';
  import { Icon, type IconString } from '$lib/icons';
  interface Props {
    disabled?: boolean;
    actionIcon: IconString;
    variant?: 'btn-neutral' | 'btn-primary' | 'btn-secondary';
    onAction?: () => void;
    children?: Snippet;
  }

  const { disabled = false, actionIcon, variant = 'btn-neutral', onAction, children }: Props = $props();
  let iconHoverColor = $derived(variant === 'btn-neutral' ? 'group-hover:bg-base-200' : 'group-hover:bg-neutral/50');

  function act(): void {
    if (!disabled) onAction?.();
  }

  let pr = $derived(disabled ? '!pr-0' : '!pr-1');
  let br = $derived(disabled ? 'border-r-0' : '');
</script>

<button
  onclick={act}
  onkeypress={act}
  class="btn badge {variant} group transition whitespace-nowrap gap-1 {pr} {br}"
  class:pointer-events-none={disabled}
>
  {@render children?.()}

  {#if !disabled}
    <span class="btn btn-circle btn-xs btn-ghost transition {iconHoverColor}">
      <Icon icon={actionIcon} />
    </span>
  {/if}
</button>
