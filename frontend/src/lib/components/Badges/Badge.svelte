<script lang="ts" module>
  import type { Snippet } from 'svelte';
  // Add more as necessary. Should be as limited as possible to maximize consistency. https://daisyui.com/components/badge/
  export type BadgeVariant = 'badge-neutral' | 'badge-info' | 'badge-primary' | 'badge-warning' | 'badge-success' | undefined;
</script>

<script lang="ts">
  import { Icon, type IconString } from '$lib/icons';

  interface Props {
    variant?: BadgeVariant;
    icon?: IconString | undefined;
    hoverIcon?: IconString | undefined;
    outline?: boolean;
    children?: Snippet;
  }

  let {
    variant = 'badge-neutral',
    icon = undefined,
    hoverIcon = undefined,
    outline = false,
    children
  }: Props = $props();
</script>

<span
  class="badge {variant ?? ''} whitespace-nowrap inline-flex gap-2 items-center group"
  class:badge-outline={outline}
>
  {@render children?.()}
  <span class="contents" class:group-hover:hidden={!!hoverIcon}>
    <Icon {icon} />
  </span>
  <span class="hidden" class:group-hover:contents={!!hoverIcon}>
    <Icon icon={hoverIcon} />
  </span>
</span>
