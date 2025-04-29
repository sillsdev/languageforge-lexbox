<script lang="ts">
  import { createBubbler } from 'svelte/legacy';

  const bubble = createBubbler();
  import type { IconString } from '$lib/icons';
  import Badge from './Badge.svelte';

  // Add more as necessary. Should be as limited as possible to maximize consistency.
  type BadgeButtonVariant = 'badge-success' | 'badge-warning' | 'badge-neutral' | undefined;
  interface Props {
    variant?: BadgeButtonVariant;
    icon?: IconString | undefined;
    hoverIcon?: IconString | undefined;
    disabled?: boolean;
    children?: import('svelte').Snippet;
  }

  let {
    variant = undefined,
    icon = undefined,
    hoverIcon = undefined,
    disabled = false,
    children
  }: Props = $props();
</script>

<button onclick={bubble('click')} {disabled} class="badge btn btn-sm !p-0 bright transition-all border-0" class:hover:brightness-90={!disabled}>
  <Badge {variant} {icon} hoverIcon={disabled ? undefined : hoverIcon}>
    {@render children?.()}
  </Badge>
</button>
