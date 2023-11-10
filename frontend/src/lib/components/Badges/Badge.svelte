<script lang="ts" context="module">
  // Add more as necessary. Should be as limited as possible to maximize consistency. https://daisyui.com/components/badge/
  export type BadgeVariant = 'badge-neutral' | 'badge-info' | 'badge-primary' | 'badge-warning' | 'badge-success';
  export type BadgeSize = 'badge-lg' | 'badge-md';
  export function badgePadding(size: BadgeSize): string {
    return size === 'badge-lg' ? 'p-4' : 'py-2 px-2';
  }
</script>

<script lang="ts">
  import { Icon, type IconString } from '$lib/icons';

  export let type: BadgeVariant = 'badge-neutral';
  export let icon: IconString | undefined = undefined;
  export let outline = false;
  export let size: BadgeSize = 'badge-lg';
  $: padding = badgePadding(size);
</script>

<span
  class="badge {size} {type} {padding} whitespace-nowrap inline-flex gap-2 items-center"
  class:badge-outline={outline}
>
  <slot />
  <Icon {icon} />
</span>
